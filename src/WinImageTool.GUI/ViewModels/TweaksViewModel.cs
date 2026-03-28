using System.Collections.ObjectModel;
using System.Windows;
using WinImageTool.Core.Components;
using WinImageTool.Core.Tweaks;

namespace Cleanse11.ViewModels;

public class TweakEntryItem : ViewModelBase
{
    private bool _isSelected = true;
    public TweakEntry Entry { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public string Path => Entry.Path;
    public string ValueName => Entry.Name;
    public string Description => Entry.Description;
    public string ValueDisplay => Entry.Kind switch
    {
        WinImageTool.Core.Tweaks.RegistryValueKind.DWord => $"0x{Entry.Value:X} (DWORD)",
        WinImageTool.Core.Tweaks.RegistryValueKind.String => $"\"{Entry.Value}\"",
        _ => Entry.Value?.ToString() ?? ""
    };

    public TweakEntryItem(TweakEntry entry) => Entry = entry;
}

public class TweakGroupItem : ViewModelBase
{
    private bool _isSelected;
    private bool _isExpanded;
    public TweakGroup Group { get; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            Set(ref _isSelected, value);
            foreach (var entry in Entries) entry.IsSelected = value;
        }
    }

    public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }
    public string DisplayName => Group.DisplayName;
    public string Description => Group.Description;
    public int EntryCount => Group.Entries.Count;
    public string Icon => GetCategoryIcon(Group.Category);
    public string ShortLabel => GetShortLabel(Group.Category);
    public string ExpandIcon => _isExpanded ? "▼" : "▶";
    public ObservableCollection<TweakEntryItem> Entries { get; }

    public TweakGroupItem(TweakGroup group)
    {
        Group = group;
        Entries = new ObservableCollection<TweakEntryItem>(
            group.Entries.Select(e => new TweakEntryItem(e)));
    }

    private static string GetCategoryIcon(TweakCategory cat) => cat switch
    {
        TweakCategory.SystemLatency => "⏱️",
        TweakCategory.InputDevice => "🖱️",
        TweakCategory.GpuScheduling => "🎮",
        TweakCategory.Network => "🌐",
        TweakCategory.Cpu => "💻",
        TweakCategory.PowerManagement => "🔋",
        TweakCategory.SystemResponsiveness => "🚀",
        TweakCategory.BootOptimization => "👢",
        TweakCategory.SystemMaintenance => "🔧",
        TweakCategory.UiResponsiveness => "✨",
        TweakCategory.Memory => "🧠",
        TweakCategory.DirectX => "🎯",
        TweakCategory.NvidiaGpu => "💚",
        TweakCategory.AmdGpu => "❤️",
        TweakCategory.SsdNvme => "💾",
        _ => "⚡"
    };

    private static string GetShortLabel(TweakCategory cat) => cat switch
    {
        TweakCategory.SystemLatency => "Latency",
        TweakCategory.InputDevice => "Input",
        TweakCategory.GpuScheduling => "GPU",
        TweakCategory.Network => "Network",
        TweakCategory.Cpu => "CPU",
        TweakCategory.PowerManagement => "Power",
        TweakCategory.SystemResponsiveness => "Response",
        TweakCategory.BootOptimization => "Boot",
        TweakCategory.SystemMaintenance => "Maint",
        TweakCategory.UiResponsiveness => "UI",
        TweakCategory.Memory => "Memory",
        TweakCategory.DirectX => "DirectX",
        TweakCategory.NvidiaGpu => "NVIDIA",
        TweakCategory.AmdGpu => "AMD",
        TweakCategory.SsdNvme => "SSD",
        _ => "Other"
    };

    public IEnumerable<TweakEntry> GetSelectedEntries()
        => Entries.Where(e => e.IsSelected).Select(e => e.Entry);
}

public class TweaksViewModel : ViewModelBase
{
    private bool _isBusy;
    private string _status = string.Empty;

    public ObservableCollection<TweakGroupItem> TweakGroups { get; } = [];
    public ObservableCollection<string> Log { get; } = [];

    public bool IsBusy   { get => _isBusy;  set { Set(ref _isBusy, value); ApplyCommand.RaiseCanExecuteChanged(); SelectAllCommand.RaiseCanExecuteChanged(); } }
    public string Status { get => _status;  set => Set(ref _status, value); }

    public RelayCommand ApplyCommand     { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearAllCommand  { get; }
    public RelayCommand ClearLogCommand  { get; }

    public TweaksViewModel()
    {
        foreach (var g in TweakCatalog.All)
            TweakGroups.Add(new TweakGroupItem(g));

        ApplyCommand     = new RelayCommand(Apply,     () => !IsBusy && TweakGroups.Any(g => g.IsSelected));
        SelectAllCommand = new RelayCommand(SelectAll, () => !IsBusy);
        ClearAllCommand  = new RelayCommand(ClearAll);
        ClearLogCommand  = new RelayCommand(Log.Clear);
    }

    private void SelectAll() { foreach (var g in TweakGroups) g.IsSelected = true; ApplyCommand.RaiseCanExecuteChanged(); }
    private void ClearAll()  { foreach (var g in TweakGroups) g.IsSelected = false; ApplyCommand.RaiseCanExecuteChanged(); }

    private void Apply()
    {
        var selectedEntries = TweakGroups
            .Where(g => g.IsSelected)
            .SelectMany(g => g.GetSelectedEntries())
            .ToList();

        if (selectedEntries.Count == 0)
        {
            Status = "No tweaks selected.";
            return;
        }

        IsBusy = true;
        Log.Clear();
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() =>
        {
            Log.Add(msg);
            Status = msg;
        }));

        Task.Run(() =>
        {
            try
            {
                var applicator = new TweakApplicator();
                int applied = 0;
                foreach (var entry in selectedEntries)
                {
                    applicator.ApplyEntry(entry, progress);
                    applied++;
                }
                Application.Current.Dispatcher.Invoke(() => 
                    Status = $"Applied {applied} registry tweak(s). Restart recommended.");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => 
                { 
                    Log.Add($"Error: {ex.Message}"); 
                    Status = $"Error: {ex.Message}"; 
                });
            }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }

    public async Task ApplyFullPresetAsync(FullPreset preset, Action<string>? onProgress = null)
    {
        foreach (var g in TweakGroups) g.IsSelected = true;
        var selectedEntries = TweakGroups
            .Where(g => g.IsSelected)
            .SelectMany(g => g.GetSelectedEntries())
            .ToList();

        if (selectedEntries.Count == 0)
        {
            onProgress?.Invoke("Tweaks: No tweaks to apply.");
            return;
        }

        IsBusy = true;
        Log.Clear();
        onProgress?.Invoke($"Tweaks: Applying {selectedEntries.Count} tweak(s)...");

        var progress = new Progress<string>(msg =>
        {
            Log.Add(msg);
            onProgress?.Invoke(msg);
        });

        try
        {
            var applicator = new TweakApplicator();
            await Task.Run(() =>
            {
                int applied = 0;
                foreach (var entry in selectedEntries)
                {
                    applicator.ApplyEntry(entry, progress);
                    applied++;
                    onProgress?.Invoke($"Tweaks: Applied {applied}/{selectedEntries.Count}");
                }
            });
            onProgress?.Invoke("Tweaks: ✓ Complete. Restart recommended.");
        }
        catch (Exception ex)
        {
            onProgress?.Invoke($"Tweaks: ✗ {ex.Message}");
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }
    }
}
