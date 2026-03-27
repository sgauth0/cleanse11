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

public class FeatureItem : ViewModelBase
{
    private bool _isSelected;
    public FeatureInfo Info { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public string Name => Info.Name;
    public string State => Info.State;
    public RemoveSafety Safety => Info.Safety;

    public FeatureItem(FeatureInfo info) { Info = info; }
}

public class ComponentsViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private string _filterText = string.Empty;
    private bool _showFeatures;
    private bool _isBusy;
    private string _status = string.Empty;
    private ComponentItem? _selectedComponent;
    private FeatureItem? _selectedFeature;
    private ComponentPreset? _selectedPreset;

    public ObservableCollection<ComponentItem>  Components { get; } = [];
    public ObservableCollection<FeatureItem>     Features   { get; } = [];
    public ObservableCollection<ComponentPreset> Presets   { get; } = new(ComponentPresets.All);

    public string FilterText   { get => _filterText;  set { Set(ref _filterText, value);   ApplyFilter(); } }
    public bool ShowFeatures   { get => _showFeatures; set { Set(ref _showFeatures, value); Notify(nameof(ShowPackages)); } }
    public bool ShowPackages   => !_showFeatures;
    public bool IsBusy         { get => _isBusy;  set => Set(ref _isBusy, value); }
    public string Status       { get => _status;  set => Set(ref _status, value); }
    public bool ShowMountHint  => string.IsNullOrEmpty(_mountPath);

    public ComponentPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { Set(ref _selectedPreset, value); }
    }

    public ComponentItem? SelectedComponent
    {
        get => _selectedComponent;
        set { Set(ref _selectedComponent, value); RemovePackageCommand.RaiseCanExecuteChanged(); }
    }

    public FeatureItem? SelectedFeature
    {
        get => _selectedFeature;
        set { Set(ref _selectedFeature, value); DisableFeatureCommand.RaiseCanExecuteChanged(); }
    }

    public RelayCommand LoadCommand          { get; }
    public RelayCommand RemovePackageCommand { get; }
    public RelayCommand DisableFeatureCommand { get; }
    public RelayCommand ApplyPresetCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearAllCommand { get; }

    private string _mountPath = string.Empty;
    private List<ComponentItem>  _allComponents = [];
    private List<FeatureItem>    _allFeatures   = [];

    public ComponentsViewModel(DismService dism)
    {
        _dism = dism;
        LoadCommand           = new RelayCommand(Load,           () => !IsBusy);
        RemovePackageCommand  = new RelayCommand(RemovePackage,  () => !IsBusy && SelectedComponent != null);
        DisableFeatureCommand = new RelayCommand(DisableFeature, () => !IsBusy && SelectedFeature != null);
        ApplyPresetCommand = new RelayCommand(ApplyPreset, () => !IsBusy && SelectedPreset != null);
        SelectAllCommand = new RelayCommand(SelectAll);
        ClearAllCommand = new RelayCommand(ClearAll);
    }

    public void SetMountPath(string path) 
    { 
        _mountPath = path; 
        Notify(nameof(ShowMountHint)); 
    }

    private void SelectAll()    { foreach (var c in Components) c.IsSelected = true; }
    private void ClearAll()     { foreach (var c in Components) c.IsSelected = false; }

    private void ApplyPreset()
    {
        if (SelectedPreset == null) return;
        ClearAll();
        foreach (var item in Components)
        {
            var lower = item.Name.ToLowerInvariant();
            foreach (var pattern in SelectedPreset.MatchPatterns)
            {
                if (lower.Contains(pattern.ToLowerInvariant()))
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }
        var count = Components.Count(c => c.IsSelected);
        Status = $"Preset '{SelectedPreset.Name}' applied — {count} package(s) selected. Click 'Remove Selected' to proceed.";
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_mountPath)) { Status = "No image mounted. Mount an image first."; return; }
        if (!Directory.Exists(_mountPath)) { Status = $"Mount path does not exist: {_mountPath}"; return; }
        IsBusy = true;
        Status = $"Loading from: {_mountPath}";

        Task.Run(() =>
        {
            try
            {
                var mgr = new ComponentManager(_dism);
                var comps = mgr.ListComponents(_mountPath);
                var feats = mgr.ListFeatures(_mountPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allComponents = comps.Select(c => new ComponentItem(c)).ToList();
                    _allFeatures   = feats.Select(f => new FeatureItem(f)).ToList();
                    ApplyFilter();
                    Status = $"{comps.Count} packages, {feats.Count} features";
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
        Features.Clear();
        var f = _filterText.ToLowerInvariant();
        foreach (var c in _allComponents.Where(c => string.IsNullOrEmpty(f) || c.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            Components.Add(c);
        foreach (var ft in _allFeatures.Where(ft => string.IsNullOrEmpty(f) || ft.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            Features.Add(ft);
    }

    private void RemovePackage()
    {
        if (SelectedComponent == null) return;
        var name = SelectedComponent.Name;
        IsBusy = true;
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() => Status = msg));
        Task.Run(() =>
        {
            try
            {
                new ComponentManager(_dism).RemovePackage(_mountPath, name, progress);
                Application.Current.Dispatcher.Invoke(() => { _allComponents.RemoveAll(c => c.Name == name); ApplyFilter(); });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => Status = $"Error: {ex.Message}");
            }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }

    private void DisableFeature()
    {
        if (SelectedFeature == null) return;
        var name = SelectedFeature.Name;
        IsBusy = true;
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() => Status = msg));
        Task.Run(() =>
        {
            try
            {
                new ComponentManager(_dism).DisableFeature(_mountPath, name, progress);
                Application.Current.Dispatcher.Invoke(Load);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => Status = $"Error: {ex.Message}");
            }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }
}
