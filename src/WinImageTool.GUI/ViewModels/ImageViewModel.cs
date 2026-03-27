using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Cleanse11.Views;
using WinImageTool.Core.Imaging;
using WinImageTool.Core.Settings;

namespace Cleanse11.ViewModels;

public class ImageViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private WimFileInfo? _wimFile;
    private string? _extractedWimPath;
    private WimImageInfo? _selectedImage;
    private string _mountPath = string.Empty;
    private string _status = string.Empty;
    private string _progressStep = string.Empty;
    private bool _isBusy;
    private bool _isMounted;

    public ObservableCollection<WimImageInfo> Images { get; } = [];

    public WimImageInfo? SelectedImage
    {
        get => _selectedImage;
        set
        {
            Set(ref _selectedImage, value);
            MountCommand.RaiseCanExecuteChanged();
            DeleteEditionCommand.RaiseCanExecuteChanged();
        }
    }

    public string MountPath
    {
        get => _mountPath;
        set { Set(ref _mountPath, value); MountCommand.RaiseCanExecuteChanged(); }
    }

    public string ScratchPath
    {
        get => _scratchPath;
        set { Set(ref _scratchPath, value); }
    }

    private string _scratchPath = string.Empty;

    public string Status { get => _status; set { Set(ref _status, value); Notify(nameof(HasStatus)); } }
    public string ProgressStep { get => _progressStep; set => Set(ref _progressStep, value); }
    public bool IsBusy { get => _isBusy; set { Set(ref _isBusy, value); MountCommand.RaiseCanExecuteChanged(); UnmountCommand.RaiseCanExecuteChanged(); DeleteEditionCommand.RaiseCanExecuteChanged(); } }
    public bool IsMounted { get => _isMounted; set { Set(ref _isMounted, value); MountCommand.RaiseCanExecuteChanged(); UnmountCommand.RaiseCanExecuteChanged(); Notify(nameof(MountStateChanged)); Notify(nameof(CanMount)); } }

    public bool HasImageLoaded => Images.Count > 0;
    public bool CanMount => HasImageLoaded && !IsMounted;
    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

    public event Action? MountStateChanged;

    public string? LoadedFilePath => _wimFile?.FilePath;

    public ObservableCollection<string> RecentMountPaths { get; } = [];

    public RelayCommand BrowseImageCommand { get; }
    public RelayCommand BrowseMountCommand { get; }
    public RelayCommand BrowseScratchCommand { get; }
    public RelayCommand MountCommand { get; }
    public RelayCommand UnmountCommand { get; }
    public RelayCommand UnmountDiscardCommand { get; }
    public RelayCommand DeleteEditionCommand { get; }
    public RelayCommand ConnectMountedCommand { get; }

    private readonly AppSettings _settings;

    public ImageViewModel(DismService dism, AppSettings settings)
    {
        _dism = dism;
        _settings = settings;
        foreach (var p in settings.RecentMountPaths)
            RecentMountPaths.Add(p);

        BrowseImageCommand = new RelayCommand(BrowseImage);
        BrowseMountCommand = new RelayCommand(BrowseMount);
        BrowseScratchCommand = new RelayCommand(BrowseScratch);
        MountCommand = new RelayCommand(DoMount, () => !IsBusy && !IsMounted && SelectedImage != null && !string.IsNullOrWhiteSpace(MountPath));
        UnmountCommand = new RelayCommand(DoUnmount, () => !IsBusy && IsMounted);
        UnmountDiscardCommand = new RelayCommand(DoUnmountDiscard, () => !IsBusy && IsMounted);
        DeleteEditionCommand = new RelayCommand(DeleteEdition, () => !IsBusy && SelectedImage != null && _wimFile != null && Images.Count > 1);
        ConnectMountedCommand = new RelayCommand(ConnectToMountedImage, () => !IsBusy);
    }

    private void BrowseImage()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open Windows Image",
            Filter = "Image files (*.wim;*.esd;*.iso)|*.wim;*.esd;*.iso|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        var oldExtractedPath = _extractedWimPath;
        _extractedWimPath = null;

        Images.Clear();
        Notify(nameof(HasImageLoaded));
        Notify(nameof(CanMount));
        Status = "Loading image info...";
        IsBusy = true;

        Task.Run(() =>
        {
            try
            {
                var filePath = dlg.FileName;

                if (IsoExtractor.IsIsoFile(filePath))
                {
                    var targetDir = !string.IsNullOrWhiteSpace(MountPath)
                        ? MountPath
                        : Path.GetDirectoryName(filePath);

                    var progress = new Progress<string>(msg =>
                        Application.Current.Dispatcher.Invoke(() => Status = msg));
                    filePath = IsoExtractor.ExtractWimFromIso(filePath, targetDir, progress);
                    _extractedWimPath = filePath;
                }

                using var wim = new WimManager();
                var info = wim.OpenWim(filePath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _wimFile = info;
                    foreach (var img in info.Images) Images.Add(img);
                    if (Images.Count > 0) SelectedImage = Images[0];
                    Status = $"Loaded {info.Images.Count} image(s) from {Path.GetFileName(dlg.FileName)}";
                    Notify(nameof(LoadedFilePath));
                    Notify(nameof(HasImageLoaded));
                    Notify(nameof(CanMount));
                });

                if (!string.IsNullOrEmpty(oldExtractedPath) && File.Exists(oldExtractedPath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(oldExtractedPath);
                        File.SetAttributes(oldExtractedPath, FileAttributes.Normal);
                        File.Delete(oldExtractedPath);
                        if (!string.IsNullOrEmpty(dir) && dir.Contains("WimCache", StringComparison.OrdinalIgnoreCase))
                            try { Directory.Delete(dir, recursive: true); } catch { }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => Status = $"Error: {ex.Message}");
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        });
    }

    private void BrowseMount()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select or create a mount directory",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            MountPath = dlg.SelectedPath;
            AddRecentPath(MountPath);
        }
    }

    private void AddRecentPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentMountPaths.Remove(path);
        RecentMountPaths.Insert(0, path);
        while (RecentMountPaths.Count > 10) RecentMountPaths.RemoveAt(RecentMountPaths.Count - 1);
        _settings.RecentMountPaths.Clear();
        foreach (var p in RecentMountPaths) _settings.RecentMountPaths.Add(p);
        _settings.Save();
    }

    private void BrowseScratch()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select scratch directory (DISM temp space, needs ~5GB free)",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            ScratchPath = dlg.SelectedPath;
    }

    private void ConnectToMountedImage()
    {
        IsBusy = true;
        Status = "Looking for mounted images...";

        Task.Run(async () =>
        {
            try
            {
                var psi = new ProcessStartInfo("dism.exe", "/Get-MountedImageInfo")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) throw new InvalidOperationException("Failed to start DISM");

                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var mountedImages = new List<MountedImageItem>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("Mount Dir :", StringComparison.OrdinalIgnoreCase))
                    {
                        var path = line.Substring("Mount Dir :".Length).Trim();
                        var imageName = "";
                        var ready = false;
                        if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("Image :", StringComparison.OrdinalIgnoreCase))
                            imageName = lines[++i].Trim().Substring("Image :".Length).Trim();
                        if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("Ready :", StringComparison.OrdinalIgnoreCase))
                            ready = lines[++i].Trim().Substring("Ready :".Length).Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase);

                        if (!string.IsNullOrEmpty(path))
                            mountedImages.Add(new MountedImageItem(path, imageName, ready));
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsBusy = false;
                    if (mountedImages.Count == 0)
                    {
                        Status = "No mounted images found.";
                        return;
                    }

                    if (mountedImages.Count == 1)
                    {
                        MountPath = mountedImages[0].Path;
                        IsMounted = true;
                        Status = $"Connected to: {mountedImages[0].Path}";
                        AddRecentPath(MountPath);
                        MountStateChanged?.Invoke();
                    }
                    else
                    {
                        var selection = new SelectMountDialog(mountedImages);
                        if (selection.ShowDialog() == true && !string.IsNullOrEmpty(selection.SelectedPath))
                        {
                            MountPath = selection.SelectedPath;
                            IsMounted = true;
                            Status = $"Connected to: {MountPath}";
                            AddRecentPath(MountPath);
                            MountStateChanged?.Invoke();
                        }
                        else
                        {
                            Status = "No image selected.";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = $"Error: {ex.Message}";
                    IsBusy = false;
                });
            }
        });
    }

    private async Task<(int exitCode, string stdout, string stderr)> RunDismAsync(
        string args,
        CancellationToken ct,
        Action<string>? onLine = null)
    {
        var psi = new ProcessStartInfo("dism.exe", args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) throw new InvalidOperationException("Failed to start DISM");

        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        var lockObj = new object();

        DataReceivedEventHandler outputHandler = (_, e) =>
        {
            if (e.Data == null) return;
            var line = e.Data;
            lock (lockObj) { stdoutLines.Add(line); }
            onLine?.Invoke(line);
        };

        DataReceivedEventHandler errorHandler = (_, e) =>
        {
            if (e.Data == null) return;
            var line = e.Data;
            lock (lockObj) { stderrLines.Add(line); }
            onLine?.Invoke(line);
        };

        proc.OutputDataReceived += outputHandler;
        proc.ErrorDataReceived += errorHandler;
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        var waitTask = Task.Run(() => proc.WaitForExit(), ct);
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30), ct);

        var completedTask = await Task.WhenAny(waitTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            try { proc.Kill(true); } catch { }
            throw new OperationCanceledException("DISM timed out after 30 minutes");
        }

        string stdout, stderr;
        lock (lockObj)
        {
            stdout = string.Join(Environment.NewLine, stdoutLines);
            stderr = string.Join(Environment.NewLine, stderrLines);
        }

        int exitCode;
        try { exitCode = proc.ExitCode; } catch { exitCode = -1; }
        return (exitCode, stdout, stderr);
    }

    private void DoMount()
    {
        if (SelectedImage == null || _wimFile == null) return;
        IsBusy = true;
        Status = $"Mounting image {SelectedImage.Index} ({SelectedImage.Name})...";
        ProgressStep = "🔗 Preparing mount environment...";

        try { Directory.CreateDirectory(MountPath); } catch { }

        var startTime = DateTime.Now;
        var spinnerIndex = 0;
        var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerTimer = new System.Threading.Timer(_ =>
        {
            if (!IsBusy) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var elapsed = DateTime.Now - startTime;
                ProgressStep = $"{spinner[spinnerIndex % spinner.Length]} Mounting... {elapsed:mm\\:ss}";
                spinnerIndex++;
            });
        }, null, 300, 300);

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

        Task.Run(async () =>
        {
            try
            {
                var args = $"/Mount-Image /ImageFile:\"{_wimFile.FilePath}\" /Index:{SelectedImage.Index} /MountDir:\"{MountPath}\"";
                if (!string.IsNullOrWhiteSpace(ScratchPath))
                    args += $" /ScratchDir:\"{ScratchPath}\"";

                var (exitCode, stdout, stderr) = await RunDismAsync(args, cts.Token, line =>
                {
                    var parsed = ParseDismProgress(line.Trim());
                    if (!string.IsNullOrEmpty(parsed))
                        Application.Current.Dispatcher.Invoke(() => ProgressStep = parsed);
                });

                spinnerTimer.Dispose();

                if (exitCode != 0)
                {
                    var err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                    throw new InvalidOperationException($"DISM failed (exit {exitCode}): {err}");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsMounted = true;
                    Status = $"✓ Mounted at {MountPath}";
                    ProgressStep = "";
                    AddRecentPath(MountPath);
                    MountStateChanged?.Invoke();
                });
            }
            catch (OperationCanceledException)
            {
                spinnerTimer.Dispose();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = "✗ Mount timed out after 30 minutes.";
                    ProgressStep = "";
                });
            }
            catch (Exception ex)
            {
                spinnerTimer.Dispose();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = $"✗ Mount failed: {ex.Message}";
                    ProgressStep = "";
                });
            }
            finally
            {
                cts.Dispose();
                Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        });
    }

    private string ParseDismProgress(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return "";

        // Mount-related
        if (line.Contains("Mounting", StringComparison.OrdinalIgnoreCase))
            return "🔗 Mounting image...";
        if (line.Contains("Copying", StringComparison.OrdinalIgnoreCase))
            return "📋 Copying files...";
        if (line.Contains("Applying", StringComparison.OrdinalIgnoreCase))
            return "⚙️ Applying image...";
        if (line.Contains("Processing", StringComparison.OrdinalIgnoreCase))
            return "📦 Processing image...";
        if (line.Contains("The operation completed", StringComparison.OrdinalIgnoreCase) || line.Contains("Successfully", StringComparison.OrdinalIgnoreCase))
            return "✅ Operation completed";
        if (line.Contains("Image mounted successfully", StringComparison.OrdinalIgnoreCase))
            return "🔗 Image mounted successfully";

        // Unmount with save
        if (line.Contains("Flushing", StringComparison.OrdinalIgnoreCase))
            return "💾 Saving changes to disk...";
        if (line.Contains("Saving image", StringComparison.OrdinalIgnoreCase) || line.Contains("Saving the", StringComparison.OrdinalIgnoreCase))
            return "📝 Writing image data...";
        if (line.Contains("Unmounting", StringComparison.OrdinalIgnoreCase))
            return "📤 Unmounting image...";
        if (line.Contains("Finalizing", StringComparison.OrdinalIgnoreCase))
            return "✅ Finalizing...";
        if (line.Contains("Removing mount", StringComparison.OrdinalIgnoreCase))
            return "🧹 Cleaning up mount point...";
        if (line.Contains("Delete the", StringComparison.OrdinalIgnoreCase) || line.Contains("Deleting the", StringComparison.OrdinalIgnoreCase))
            return "🗑️ Removing temporary files...";
        if (line.Contains("Exporting", StringComparison.OrdinalIgnoreCase))
            return "📤 Exporting image...";
        if (line.Contains("Capturing", StringComparison.OrdinalIgnoreCase))
            return "📸 Capturing image...";
        if (line.Contains("Adding package", StringComparison.OrdinalIgnoreCase))
            return "📦 Adding package...";
        if (line.Contains("Installing package", StringComparison.OrdinalIgnoreCase))
            return "📥 Installing package...";
        if (line.Contains("Disabling", StringComparison.OrdinalIgnoreCase))
            return "⚙️ Disabling feature...";
        if (line.Contains("Enabling", StringComparison.OrdinalIgnoreCase))
            return "⚙️ Enabling feature...";
        if (line.Contains("Removing", StringComparison.OrdinalIgnoreCase) && line.Contains("package", StringComparison.OrdinalIgnoreCase))
            return "🗑️ Removing package...";

        // Percentage progress
        if (line.Contains("%"))
        {
            var idx = line.LastIndexOf('%');
            return "Progress: " + line.Substring(Math.Max(0, idx - 3), 4).Trim() + "%";
        }

        // Fallback — show a truncated snippet of the actual line
        return line.Length > 75 ? "⏳ " + line.Substring(0, 75) : "⏳ " + line;
    }

    private void DoUnmount() => Unmount(commit: true);
    private void DoUnmountDiscard() => Unmount(commit: false);

    private void Unmount(bool commit)
    {
        IsBusy = true;
        Status = commit ? "Saving and unmounting..." : "Discarding and unmounting...";
        ProgressStep = commit ? "💾 Getting ready to save..." : "🧹 Getting ready to discard...";

        var startTime = DateTime.Now;
        var spinnerIndex = 0;
        var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerTimer = new System.Threading.Timer(_ =>
        {
            if (!IsBusy) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var elapsed = DateTime.Now - startTime;
                ProgressStep = $"{spinner[spinnerIndex % spinner.Length]} {(commit ? "Saving" : "Discarding")}... {elapsed:mm\\:ss}";
                spinnerIndex++;
            });
        }, null, 300, 300);

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        var savedMountPath = MountPath;

        Task.Run(async () =>
        {
            try
            {
                var commitStr = commit ? "/Commit" : "/Discard";
                var args = $"/Unmount-Image /MountDir:\"{savedMountPath}\" {commitStr}";

                var (exitCode, stdout, stderr) = await RunDismAsync(args, cts.Token, line =>
                {
                    var parsed = ParseDismProgress(line.Trim());
                    if (!string.IsNullOrEmpty(parsed))
                        Application.Current.Dispatcher.Invoke(() => ProgressStep = parsed);
                });

                spinnerTimer.Dispose();

                if (exitCode != 0)
                {
                    var err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                    throw new InvalidOperationException($"DISM failed (exit {exitCode}): {err}");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsMounted = false;
                    Status = commit ? "✓ Unmounted and saved." : "✓ Unmounted (changes discarded).";
                    ProgressStep = "";
                    MountStateChanged?.Invoke();
                });
            }
            catch (OperationCanceledException)
            {
                spinnerTimer.Dispose();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = "✗ Unmount timed out after 30 minutes.";
                    ProgressStep = "";
                });
            }
            catch (Exception ex)
            {
                spinnerTimer.Dispose();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = $"✗ Unmount failed: {ex.Message}";
                    ProgressStep = "";
                });
            }
            finally
            {
                cts.Dispose();
                Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        });
    }

    private void DeleteEdition()
    {
        if (SelectedImage == null || _wimFile == null) return;
        if (Images.Count <= 1)
        {
            Status = "Cannot delete the only edition in the image.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Delete '{SelectedImage.Name}' from the WIM file?\n\nThis cannot be undone!",
            "Delete Edition",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        var indexToDelete = SelectedImage.Index;
        var name = SelectedImage.Name;
        IsBusy = true;
        Status = $"Deleting edition {indexToDelete} ({name})...";

        Task.Run(async () =>
        {
            try
            {
                var psi = new ProcessStartInfo("dism.exe",
                    $"/Delete-Image /ImageFile:\"{_wimFile.FilePath}\" /Index:{indexToDelete}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start DISM");
                var stdout = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (proc.ExitCode != 0)
                {
                    var err = await proc.StandardError.ReadToEndAsync();
                    var displayErr = string.IsNullOrWhiteSpace(err) ? stdout : err;
                    throw new InvalidOperationException($"DISM failed (exit {proc.ExitCode}): {displayErr}");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Images.Remove(SelectedImage);
                    foreach (var img in Images)
                    {
                        if (img.Index > indexToDelete)
                            img.Index--;
                    }
                    if (Images.Count > 0)
                        SelectedImage = Images[0];
                    Status = $"Deleted '{name}'. {Images.Count} editions remaining.";
                    DeleteEditionCommand.RaiseCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => Status = $"Delete failed: {ex.Message}");
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        });
    }
}
