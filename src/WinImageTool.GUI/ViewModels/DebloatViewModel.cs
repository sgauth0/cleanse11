using System.Collections.ObjectModel;
using System.Windows;
using WinImageTool.Core.Bloat;
using WinImageTool.Core.Components;

namespace Cleanse11.ViewModels;

public class BloatPackageItem : ViewModelBase
{
    private bool _isSelected;
    public BloatPackage Package { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public string DisplayName => Package.DisplayName;
    public string Category    => Package.Category.ToString();
    public string Prefix      => Package.Prefix;

    public BloatPackageItem(BloatPackage pkg)
    {
        Package    = pkg;
        _isSelected = pkg.DefaultSelected;
    }
}

public class DebloatViewModel : ViewModelBase
{
    private bool _isBusy;
    private string _status = string.Empty;
    private string _mountPath = string.Empty;
    private bool _removeEdge = true;
    private bool _removeOneDrive = true;
    private bool _applyOfflineTweaks = true;
    private bool _removeScheduledTasks = true;

    public ObservableCollection<BloatPackageItem> Packages { get; } = [];
    public ObservableCollection<string> Log { get; } = [];

    public bool IsBusy   { get => _isBusy;  set { Set(ref _isBusy, value); ApplyCommand.RaiseCanExecuteChanged(); } }
    public string Status { get => _status;  set => Set(ref _status, value); }
    public bool RemoveEdge           { get => _removeEdge;           set => Set(ref _removeEdge, value); }
    public bool RemoveOneDrive       { get => _removeOneDrive;       set => Set(ref _removeOneDrive, value); }
    public bool ApplyOfflineTweaks   { get => _applyOfflineTweaks;   set => Set(ref _applyOfflineTweaks, value); }
    public bool RemoveScheduledTasks { get => _removeScheduledTasks; set => Set(ref _removeScheduledTasks, value); }

    public RelayCommand ApplyCommand     { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearAllCommand  { get; }
    public RelayCommand Tiny11PresetCommand { get; }

    public DebloatViewModel()
    {
        foreach (var pkg in BloatwareList.All)
            Packages.Add(new BloatPackageItem(pkg));

        ApplyCommand       = new RelayCommand(Apply,          () => !IsBusy);
        SelectAllCommand   = new RelayCommand(SelectAll);
        ClearAllCommand    = new RelayCommand(ClearAll);
        Tiny11PresetCommand = new RelayCommand(ApplyTiny11Preset);
    }

    public void SetMountPath(string path) => _mountPath = path;

    private void SelectAll()    { foreach (var p in Packages) p.IsSelected = true; }
    private void ClearAll()     { foreach (var p in Packages) p.IsSelected = false; }

    private void ApplyTiny11Preset()
    {
        // Select all by default; only deselect Windows Terminal and Paint
        foreach (var p in Packages)
            p.IsSelected = p.Package.DefaultSelected;
        RemoveEdge           = true;
        RemoveOneDrive       = true;
        ApplyOfflineTweaks   = true;
        RemoveScheduledTasks = true;
        Status = "Tiny11 preset applied. Click 'Apply Debloat' to run.";
    }

    private void Apply()
    {
        if (string.IsNullOrEmpty(_mountPath)) { Status = "No image mounted."; return; }

        IsBusy = true;
        Log.Clear();
        var selected = Packages.Where(p => p.IsSelected).Select(p => p.Package).ToList();
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() =>
        {
            Log.Add(msg);
            Status = msg;
        }));

        var removeEdge      = RemoveEdge;
        var removeOneDrive  = RemoveOneDrive;
        var offlineTweaks   = ApplyOfflineTweaks;
        var removeTasks     = RemoveScheduledTasks;
        var mountPath       = _mountPath;

        Task.Run(() =>
        {
            try
            {
                var mgr = new BloatwareManager();
                mgr.RemovePackages(mountPath, selected, progress);
                if (removeEdge || removeOneDrive)
                    mgr.RemoveEdgeAndOneDrive(mountPath, progress);
                if (removeTasks)
                    mgr.RemoveScheduledTasks(mountPath, progress);
                if (offlineTweaks)
                    new OfflineTweaks(mountPath).Apply(progress);

                Application.Current.Dispatcher.Invoke(() => Status = "Debloat complete. Unmount and save to commit changes.");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Log.Add($"Error: {ex.Message}"); Status = $"Error: {ex.Message}"; });
            }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }

    public async Task ApplyFullPresetAsync(FullPreset preset, Action<string>? onProgress = null)
    {
        if (string.IsNullOrEmpty(_mountPath))
        {
            onProgress?.Invoke("Debloat: No mount path.");
            return;
        }

        foreach (var p in Packages)
            p.IsSelected = p.Package.DefaultSelected;
        RemoveEdge = true;
        RemoveOneDrive = true;
        ApplyOfflineTweaks = true;
        RemoveScheduledTasks = true;

        IsBusy = true;
        Log.Clear();
        var selected = Packages.Where(p => p.IsSelected).Select(p => p.Package).ToList();
        onProgress?.Invoke($"Debloat: Applying to {selected.Count} package(s)...");

        var progress = new Progress<string>(msg =>
        {
            Log.Add(msg);
            onProgress?.Invoke(msg);
        });

        try
        {
            var mgr = new BloatwareManager();
            await Task.Run(() =>
            {
                mgr.RemovePackages(_mountPath, selected, progress);
                if (RemoveEdge || RemoveOneDrive)
                    mgr.RemoveEdgeAndOneDrive(_mountPath, progress);
                if (RemoveScheduledTasks)
                    mgr.RemoveScheduledTasks(_mountPath, progress);
                if (ApplyOfflineTweaks)
                    new OfflineTweaks(_mountPath).Apply(progress);
            });
            onProgress?.Invoke("Debloat: ✓ Complete.");
        }
        catch (Exception ex)
        {
            onProgress?.Invoke($"Debloat: ✗ {ex.Message}");
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }
    }
}
