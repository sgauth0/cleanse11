// Tweak sources (MIT licensed): https://github.com/emylfy/simplify11
// Original sources credited per tweak: AlchemyTweaks/Verified-Tweaks, SanGraphic/QuickBoost,
// UnLovedCookie/CoutX, Snowfliger/SyncOS, denis-g/windows10-latency-optimization

namespace WinImageTool.Core.Tweaks;

using H = RegistryHive;
using K = RegistryValueKind;

public static class TweakCatalog
{
    public static IReadOnlyList<TweakGroup> All { get; } =
    [
        new(TweakCategory.SystemLatency, "System Latency",
            "Reduce interrupt and timer latency for lower system latency",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "InterruptSteeringDisabled", 1, K.DWord, H.LocalMachine, "Disable interrupt steering for lower latency"),
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "SerializeTimerExpiration",   1, K.DWord, H.LocalMachine, "Serialize timer expiration for better system timing"),
            ]),

        new(TweakCategory.InputDevice, "Input Device Optimization",
            "Reduce mouse and keyboard input latency",
            [
                new("SYSTEM\\CurrentControlSet\\Services\\mouclass\\Parameters", "MouseDataQueueSize",    20,    K.DWord,  H.LocalMachine, "Optimize mouse input buffer size"),
                new("SYSTEM\\CurrentControlSet\\Services\\kbdclass\\Parameters", "KeyboardDataQueueSize", 20,    K.DWord,  H.LocalMachine, "Optimize keyboard input buffer size"),
                new("Control Panel\\Accessibility",                               "StickyKeys",            "506", K.String, H.CurrentUser,  "Disable StickyKeys popup"),
                new("Control Panel\\Accessibility\\ToggleKeys",                   "Flags",                 "58",  K.String, H.CurrentUser,  "Disable ToggleKeys audio indicator"),
                new("Control Panel\\Accessibility\\Keyboard Response",            "DelayBeforeAcceptance", "0",   K.String, H.CurrentUser,  "Remove keyboard input delay"),
                new("Control Panel\\Accessibility\\Keyboard Response",            "AutoRepeatRate",        "0",   K.String, H.CurrentUser,  "Optimize key repeat rate"),
                new("Control Panel\\Accessibility\\Keyboard Response",            "AutoRepeatDelay",       "0",   K.String, H.CurrentUser,  "Remove key repeat delay"),
                new("Control Panel\\Accessibility\\Keyboard Response",            "Flags",                 "122", K.String, H.CurrentUser,  "Optimize keyboard response flags"),
            ]),

        new(TweakCategory.GpuScheduling, "GPU Hardware Scheduling",
            "Enable HAGS and disable preemption for reduced GPU latency",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers",           "HwSchMode",        2, K.DWord, H.LocalMachine, "Enable Hardware Accelerated GPU Scheduling (HAGS)"),
                new("SYSTEM\\ControlSet001\\Control\\GraphicsDrivers\\Scheduler",    "EnablePreemption", 0, K.DWord, H.LocalMachine, "Disable GPU preemption for better performance"),
            ]),

        new(TweakCategory.Network, "Network Optimization",
            "Disable network throttling for maximum throughput",
            [
                new("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), K.DWord, H.LocalMachine, "Disable network throttling"),
                new("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NoLazyMode",             1,                          K.DWord, H.LocalMachine, "Disable lazy mode for network operations"),
            ]),

        new(TweakCategory.Cpu, "CPU Performance",
            "Optimize Multimedia Class Scheduler for lower CPU latency",
            [
                new("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "LazyModeTimeout", 25000, K.DWord, H.LocalMachine, "Set optimal lazy mode timeout"),
                new("SYSTEM\\CurrentControlSet\\Services\\MMCSS",                                 "Start",           2,     K.DWord, H.LocalMachine, "Configure MMCSS for better performance"),
            ]),

        new(TweakCategory.PowerManagement, "Power Management",
            "Disable power throttling and energy estimation overhead",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling",                         "PowerThrottlingOff",            1, K.DWord, H.LocalMachine, "Disable power throttling"),
                new("SYSTEM\\CurrentControlSet\\Control\\Power",                                           "EnergyEstimationEnabled",       0, K.DWord, H.LocalMachine, "Disable energy estimation"),
                new("SYSTEM\\CurrentControlSet\\Control\\Power",                                           "EventProcessorEnabled",         0, K.DWord, H.LocalMachine, "Disable power event processor"),
                new("SYSTEM\\CurrentControlSet\\Control\\Power\\EnergyEstimation\\TaggedEnergy",          "DisableTaggedEnergyLogging",    1, K.DWord, H.LocalMachine, "Disable tagged energy logging"),
                new("SYSTEM\\CurrentControlSet\\Control\\Power\\EnergyEstimation\\TaggedEnergy",          "TelemetryMaxApplication",       0, K.DWord, H.LocalMachine, "Disable per-app energy telemetry"),
                new("SYSTEM\\CurrentControlSet\\Control\\Power\\EnergyEstimation\\TaggedEnergy",          "TelemetryMaxTagPerApplication", 0, K.DWord, H.LocalMachine, "Disable per-app energy tagging"),
            ]),

        new(TweakCategory.SystemResponsiveness, "System Responsiveness",
            "Prioritize foreground processes over background services",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\PriorityControl",  "Win32PrioritySeparation", 0x24, K.DWord, H.LocalMachine, "Optimize process priority (short variable intervals, high foreground boost)"),
                new("SYSTEM\\ControlSet001\\Control\\PriorityControl",       "IRQ8Priority",            1,    K.DWord, H.LocalMachine, "Set IRQ8 priority"),
                new("SYSTEM\\ControlSet001\\Control\\PriorityControl",       "IRQ16Priority",           2,    K.DWord, H.LocalMachine, "Set IRQ16 priority"),
            ]),

        new(TweakCategory.BootOptimization, "Boot Optimization",
            "Remove startup and desktop switch delays",
            [
                new("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "Startupdelayinmsec",         0, K.DWord, H.CurrentUser,  "Remove startup delay"),
                new("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",    "DelayedDesktopSwitchTimeout", 0, K.DWord, H.LocalMachine, "Remove desktop switch delay"),
            ]),

        new(TweakCategory.SystemMaintenance, "System Maintenance",
            "Disable automatic maintenance and I/O accounting overhead",
            [
                new("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 1, K.DWord, H.LocalMachine, "Disable automatic maintenance"),
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\I/O System",         "CountOperations",     0, K.DWord, H.LocalMachine, "Disable I/O operation counting"),
                new("SOFTWARE\\Policies\\Microsoft\\Windows\\fssProv",                         "EncryptProtocol",     0, K.DWord, H.LocalMachine, "Disable FSS provider encryption"),
                new("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule",               "DisableRpcOver",      1, K.DWord, H.LocalMachine, "Disable RPC over Scheduler"),
            ]),

        new(TweakCategory.UiResponsiveness, "UI Responsiveness",
            "Reduce timeouts for hung app detection and menu animation",
            [
                new("Control Panel\\Desktop", "AutoEndTasks",         "1",    K.String, H.CurrentUser,  "Auto-end hung tasks"),
                new("Control Panel\\Desktop", "HungAppTimeout",       "1000", K.String, H.CurrentUser,  "Reduce hung app timeout to 1s"),
                new("Control Panel\\Desktop", "WaitToKillAppTimeout", "2000", K.String, H.CurrentUser,  "Reduce kill wait to 2s"),
                new("Control Panel\\Desktop", "LowLevelHooksTimeout", "1000", K.String, H.CurrentUser,  "Reduce low-level hooks timeout"),
                new("Control Panel\\Desktop", "MenuShowDelay",        "0",    K.String, H.CurrentUser,  "Remove menu animation delay"),
                new("SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout", "2000", K.String, H.LocalMachine, "Reduce service kill wait to 2s"),
            ]),

        new(TweakCategory.Memory, "Memory Optimization",
            "Tune memory manager for lower latency workloads",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache",       1, K.DWord, H.LocalMachine, "Enable large system cache"),
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingCombining", 1, K.DWord, H.LocalMachine, "Disable memory page combining"),
                new("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1, K.DWord, H.LocalMachine, "Keep kernel/drivers in RAM"),
            ]),

        new(TweakCategory.DirectX, "DirectX Enhancements",
            "Enable D3D11/D3D12 multithreading and runtime optimizations",
            [
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_ENABLE_UNSAFE_COMMAND_BUFFER_REUSE",  1, K.DWord, H.LocalMachine, "Enable D3D12 command buffer reuse"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_ENABLE_RUNTIME_DRIVER_OPTIMIZATIONS", 1, K.DWord, H.LocalMachine, "Enable D3D12 runtime optimizations"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_RESOURCE_ALIGNMENT",                  1, K.DWord, H.LocalMachine, "Optimize D3D12 resource alignment"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D11_MULTITHREADED",                       1, K.DWord, H.LocalMachine, "Enable D3D11 multithreading"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_MULTITHREADED",                       1, K.DWord, H.LocalMachine, "Enable D3D12 multithreading"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D11_DEFERRED_CONTEXTS",                   1, K.DWord, H.LocalMachine, "Enable D3D11 deferred contexts"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_DEFERRED_CONTEXTS",                   1, K.DWord, H.LocalMachine, "Enable D3D12 deferred contexts"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D11_ALLOW_TILING",                        1, K.DWord, H.LocalMachine, "Enable D3D11 tiling"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D11_ENABLE_DYNAMIC_CODEGEN",              1, K.DWord, H.LocalMachine, "Enable D3D11 dynamic code generation"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_ALLOW_TILING",                        1, K.DWord, H.LocalMachine, "Enable D3D12 tiling"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_CPU_PAGE_TABLE_ENABLED",              1, K.DWord, H.LocalMachine, "Enable D3D12 CPU page table"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_HEAP_SERIALIZATION_ENABLED",          1, K.DWord, H.LocalMachine, "Enable D3D12 heap serialization"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_MAP_HEAP_ALLOCATIONS",                1, K.DWord, H.LocalMachine, "Enable D3D12 heap allocation mapping"),
                new("SOFTWARE\\Microsoft\\DirectX", "D3D12_RESIDENCY_MANAGEMENT_ENABLED",        1, K.DWord, H.LocalMachine, "Enable D3D12 residency management"),
            ]),

        new(TweakCategory.NvidiaGpu, "NVIDIA GPU Tweaks",
            "Enable per-CPU core DPC for NVIDIA drivers to reduce GPU latency",
            [
                new("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers",                        "RmGpsPsEnablePerCpuCoreDpc", 1, K.DWord, H.LocalMachine, "Enable per-CPU core DPC (GraphicsDrivers)"),
                new("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Power",                 "RmGpsPsEnablePerCpuCoreDpc", 1, K.DWord, H.LocalMachine, "Enable power-aware per-CPU core DPC"),
                new("SYSTEM\\CurrentControlSet\\Services\\nvlddmkm",                              "RmGpsPsEnablePerCpuCoreDpc", 1, K.DWord, H.LocalMachine, "Enable NVIDIA driver per-CPU core DPC"),
                new("SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\NVAPI",                       "RmGpsPsEnablePerCpuCoreDpc", 1, K.DWord, H.LocalMachine, "Enable NVIDIA API per-CPU core DPC"),
                new("SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\Global\\NVTweak",             "RmGpsPsEnablePerCpuCoreDpc", 1, K.DWord, H.LocalMachine, "Enable global NVIDIA per-CPU core DPC"),
            ]),
    ];

    public static TweakGroup? GetGroup(TweakCategory category)
        => All.FirstOrDefault(g => g.Category == category);
}
