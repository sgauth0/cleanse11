using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using WinImageTool.Core.Updates;
using WinImageTool.Core.Imaging;

namespace Cleanse11.ViewModels;

public class InstallPackage : ViewModelBase
{
    private bool _isSelected;
    public string Name { get; }
    public string Description { get; }
    public string InstallCommand { get; }

    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }

    public InstallPackage(string name, string description, string installCommand)
    {
        Name = name;
        Description = description;
        InstallCommand = installCommand;
    }
}

public class UpdatesViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private bool _isBusy;
    private string _status = string.Empty;
    private string _mountPath = string.Empty;

    public ObservableCollection<string> Log { get; } = [];
    public ObservableCollection<InstallPackage> InstallPackages { get; } = [
        new("Git",              "Latest Git for Windows (version control)",                   "winget install Git.Git --accept-package-agreements --accept-source-agreements"),
        new("Python",          "Latest Python 3 (programming runtime)",                       "winget install Python.Python.3.12 --accept-package-agreements --accept-source-agreements"),
        new("Node.js",         "Latest Node.js LTS (JavaScript runtime)",                    "winget install OpenJS.NodeJS.LTS --accept-package-agreements --accept-source-agreements"),
        new("VS Code",         "Visual Studio Code (code editor)",                          "winget install Microsoft.VisualStudioCode --accept-package-agreements --accept-source-agreements"),
        new("7-Zip",           "7-Zip file archiver",                                      "winget install 7zip.7zip --accept-package-agreements --accept-source-agreements"),
        new("PowerShell 7",    "PowerShell 7 (updated shell)",                            "winget install Microsoft.PowerShell --accept-package-agreements --accept-source-agreements"),
        new("Docker Desktop",  "Docker Desktop (containers)",                                "winget install Docker.DockerDesktop --accept-package-agreements --accept-source-agreements"),
        new("OpenClaw",       "OpenClaw AI assistant (via pip)",                           "pip install openclaw"),
        new("Ollama",         "Ollama local LLM runner",                                 "winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements"),
        new("Postman",         "Postman API client",                                       "winget install Postman.Postman --accept-package-agreements --accept-source-agreements"),
        new("Notepad++",       "Notepad++ text editor",                                    "winget install Notepad++.Notepad++ --accept-package-agreements --accept-source-agreements"),
        new(" SumatraPDF",     "SumatraPDF reader (lightweight PDF viewer)",                "winget install SumatraPDF.SumatraPDF --accept-package-agreements --accept-source-agreements"),
    ];

    public bool IsBusy   { get => _isBusy;  set => Set(ref _isBusy, value); }
    public string Status { get => _status;  set => Set(ref _status, value); }

    public RelayCommand AddUpdateFileCommand   { get; }
    public RelayCommand AddUpdateFolderCommand { get; }
    public RelayCommand ClearLogCommand        { get; }
    public RelayCommand InstallSelectedCommand { get; }
    public RelayCommand<InstallPackage> TogglePackageCommand { get; }

    public UpdatesViewModel(DismService dism)
    {
        _dism = dism;
        AddUpdateFileCommand   = new RelayCommand(AddFile,   () => !IsBusy);
        AddUpdateFolderCommand = new RelayCommand(AddFolder, () => !IsBusy);
        ClearLogCommand        = new RelayCommand(Log.Clear);
        InstallSelectedCommand = new RelayCommand(InstallSelected, () => !IsBusy && InstallPackages.Any(p => p.IsSelected));
        TogglePackageCommand  = new RelayCommand<InstallPackage>(pkg => { if (pkg != null) pkg.IsSelected = !pkg.IsSelected; });
    }

    public void SetMountPath(string path) => _mountPath = path;

    private void AddFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select Update Package",
            Filter = "Update packages (*.msu;*.cab)|*.msu;*.cab|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        RunIntegration(dlg.FileName, false);
    }

    private void AddFolder()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder containing .msu / .cab update files",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        RunIntegration(dlg.SelectedPath, true);
    }

    private void RunIntegration(string path, bool isFolder)
    {
        if (string.IsNullOrEmpty(_mountPath)) { Status = "No image mounted."; return; }
        IsBusy = true;
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() =>
        {
            Log.Add(msg);
            Status = msg;
        }));
        var integrator = new UpdateIntegrator(_dism);
        Task.Run(() =>
        {
            try
            {
                if (isFolder) integrator.IntegrateUpdatesFromFolder(_mountPath, path, progress);
                else          integrator.IntegrateUpdate(_mountPath, path, progress);
            }
            catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => { Log.Add($"Error: {ex.Message}"); Status = $"Error: {ex.Message}"; }); }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }

    private void InstallSelected()
    {
        var selected = InstallPackages.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0) return;
        IsBusy = true;
        Status = $"Installing {selected.Count} package(s)...";

        Task.Run(async () =>
        {
            foreach (var pkg in selected)
            {
                Application.Current.Dispatcher.Invoke(() => Status = $"Installing {pkg.Name}...");
                try
                {
                    var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"{pkg.InstallCommand}\"")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc == null) throw new InvalidOperationException("Failed to start powershell");
                    var output = await proc.StandardOutput.ReadToEndAsync();
                    var errors = await proc.StandardError.ReadToEndAsync();
                    proc.WaitForExit();

                    var result = proc.ExitCode == 0 ? "Installed" : "Failed";
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Log.Add($"[{result}] {pkg.Name}");
                        if (!string.IsNullOrEmpty(errors)) Log.Add($"  Error: {errors.Trim()}");
                        else if (!string.IsNullOrEmpty(output)) Log.Add($"  {output.Trim().Split('\n').FirstOrDefault()?.Trim()}");
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => Log.Add($"[Error] {pkg.Name}: {ex.Message}"));
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = $"Done. {selected.Count} package(s) processed.";
                IsBusy = false;
            });
        });
    }
}
