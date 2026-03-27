namespace WinImageTool.Core.Tweaks;

public enum TweakCategory
{
    SystemLatency,
    InputDevice,
    SsdNvme,
    GpuScheduling,
    Network,
    Cpu,
    PowerManagement,
    SystemResponsiveness,
    BootOptimization,
    SystemMaintenance,
    UiResponsiveness,
    Memory,
    DirectX,
    NvidiaGpu,
    AmdGpu,
}

public enum RegistryHive { LocalMachine, CurrentUser }
public enum RegistryValueKind { DWord, String, QWord, ExpandString }

public record TweakEntry(
    string Path,
    string Name,
    object Value,
    RegistryValueKind Kind,
    RegistryHive Hive,
    string Description);

public record TweakGroup(
    TweakCategory Category,
    string DisplayName,
    string Description,
    IReadOnlyList<TweakEntry> Entries);
