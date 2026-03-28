using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using WinImageTool.Core.Components;
using WinImageTool.Core.Imaging;

namespace Cleanse11.ViewModels;

public class ComponentItem : ViewModelBase
{
    private bool _isSelected;
    public ComponentInfo Info { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public string Name => Info.Name;
    public string State => Info.State;
    public string ReleaseType => Info.ReleaseType;
    public RemoveSafety Safety => Info.Safety;

    public ComponentItem(ComponentInfo info) { Info = info; }
}

public class ComponentsViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private string _filterText = string.Empty;
    private bool _isBusy;
    private string _status = string.Empty;
    private ComponentPreset? _selectedPreset;
    private string _mountPath = string.Empty;
    private List<ComponentItem> _allComponents = [];
    private bool _isLoaded;

    public ObservableCollection<ComponentItem> Components { get; } = [];
    public ObservableCollection<ComponentPreset> Presets { get; } = new(ComponentPresets.All);

    public string FilterText { get => _filterText; set { Set(ref _filterText, value); ApplyFilter(); } }
    public bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool ShowMountHint => string.IsNullOrEmpty(_mountPath) || !_isLoaded;
    public int SelectedCount => Components.Count(c => c.IsSelected);

    public ComponentPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { Set(ref _selectedPreset, value); }
    }

    public RelayCommand LoadCommand { get; }
    public RelayCommand ApplyPresetCommand { get; }
    public RelayCommand ApplyPresetAndRemoveCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearAllCommand { get; }

    private bool HasSelectedComponents => Components.Any(c => c.IsSelected);

    public ComponentsViewModel(DismService dism)
    {
        _dism = dism;
        LoadCommand = new RelayCommand(Load, () => !IsBusy && !string.IsNullOrEmpty(_mountPath));
        ApplyPresetCommand = new RelayCommand(ApplyPreset, () => !IsBusy && SelectedPreset != null);
        ApplyPresetAndRemoveCommand = new RelayCommand(ApplyPresetAndRemove, () => !IsBusy && SelectedPreset != null);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => !IsBusy && HasSelectedComponents);
        SelectAllCommand = new RelayCommand(SelectAll);
        ClearAllCommand = new RelayCommand(ClearAll);
    }

    public void SetMountPath(string path)
    {
        _mountPath = path;
        Notify(nameof(ShowMountHint));
        if (!_isLoaded && !string.IsNullOrEmpty(path))
            Load();
    }

    private void SelectAll()
    {
        foreach (var c in _allComponents) c.IsSelected = true;
        Notify(nameof(SelectedCount));
        Status = $"All {_allComponents.Count} items selected.";
    }

    private void ClearAll()
    {
        foreach (var c in _allComponents) c.IsSelected = false;
        Notify(nameof(SelectedCount));
        Status = "Selection cleared.";
    }

    private void ApplyPreset()
    {
        if (SelectedPreset == null) return;
        foreach (var c in _allComponents) c.IsSelected = false;

        foreach (var c in _allComponents)
        {
            var lower = c.Name.ToLowerInvariant();
            foreach (var pattern in SelectedPreset.MatchPatterns)
            {
                if (lower.Contains(pattern.ToLowerInvariant()))
                {
                    c.IsSelected = true;
                    break;
                }
            }
        }
        Notify(nameof(SelectedCount));
        var count = _allComponents.Count(c => c.IsSelected);
        Status = $"'{SelectedPreset.Name}' selected {count} item(s).";
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_mountPath)) { Status = "No image mounted."; return; }
        if (!Directory.Exists(_mountPath)) { Status = $"Mount path not found: {_mountPath}"; return; }
        IsBusy = true;
        Status = "Loading components...";

        Task.Run(() =>
        {
            try
            {
                var mgr = new ComponentManager(_dism);
                var comps = mgr.ListComponents(_mountPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allComponents = comps.Select(c => new ComponentItem(c)).ToList();
                    ApplyFilter();
                    _isLoaded = true;
                    Notify(nameof(ShowMountHint));
                    Status = $"Loaded {_allComponents.Count} packages.";
                });
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

    private void ApplyFilter()
    {
        Components.Clear();
        var f = FilterText?.ToLowerInvariant() ?? "";
        foreach (var c in _allComponents.Where(c =>
            string.IsNullOrEmpty(f) || c.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            Components.Add(c);
        Notify(nameof(SelectedCount));
    }

    public async Task ApplyFullPresetAsync(FullPreset preset, Action<string>? onProgress = null)
    {
        if (string.IsNullOrEmpty(_mountPath))
        {
            onProgress?.Invoke("Components: No mount path.");
            return;
        }

        if (!_isLoaded)
        {
            onProgress?.Invoke("Components: Loading packages...");
            Load();
            await Task.Run(() => { while (!_isLoaded && IsBusy) Thread.Sleep(100); Thread.Sleep(200); });
        }

        var name = preset switch
        {
            FullPreset.Lite => "Lite",
            FullPreset.OpenClaw => "Openclaw",
            _ => "Lite"
        };
        var presetToApply = Presets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (presetToApply == null)
        {
            onProgress?.Invoke($"Components: Preset '{name}' not found.");
            return;
        }

        SelectedPreset = presetToApply;
        ApplyPreset();
        var toRemove = _allComponents.Where(c => c.IsSelected).ToList();
        onProgress?.Invoke($"Components: Removing {toRemove.Count} package(s)...");

        if (toRemove.Count == 0)
        {
            onProgress?.Invoke("Components: No packages to remove.");
            return;
        }

        IsBusy = true;
        var mgr = new ComponentManager(_dism);
        var removed = 0;
        var failed = 0;

        foreach (var item in toRemove)
        {
            try
            {
                onProgress?.Invoke($"Components: Removing ({removed + 1}/{toRemove.Count}): {Truncate(item.Name, 50)}");
                await Task.Run(() => mgr.RemovePackage(_mountPath, item.Name, null));
                removed++;
            }
            catch
            {
                failed++;
            }
        }

        foreach (var item in toRemove)
            _allComponents.Remove(item);
        ApplyFilter();
        IsBusy = false;
        onProgress?.Invoke($"Components: ✓ Removed {removed} package(s)" + (failed > 0 ? $" ({failed} failed)" : ""));
    }

    private void ApplyPresetAndRemove()
    {
        if (SelectedPreset == null) return;
        ApplyPreset();
        RemoveSelected();
    }

    private void RemoveSelected()
    {
        var toRemove = _allComponents.Where(c => c.IsSelected).ToList();
        if (toRemove.Count == 0) return;
        IsBusy = true;
        Status = $"Removing {toRemove.Count} item(s)...";

        Task.Run(() =>
        {
            try
            {
                var mgr = new ComponentManager(_dism);
                var removed = 0;
                var failed = 0;

                foreach (var item in toRemove)
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() => Status = $"Removing ({removed + 1}/{toRemove.Count}): {Truncate(item.Name, 50)}");
                        mgr.RemovePackage(_mountPath, item.Name, null);
                        removed++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in toRemove)
                        _allComponents.Remove(item);
                    ApplyFilter();
                    Status = $"✓ Removed {removed} package(s)" + (failed > 0 ? $" ({failed} failed)" : "");
                });
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

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";
}
