// Offline image registry tweaks sourced from tiny11builder (open source)
// https://github.com/ntdevlabs/tiny11builder
// Applied to mounted offline hives: zSOFTWARE, zSYSTEM, zNTUSER, zDEFAULT

using Microsoft.Win32;

namespace WinImageTool.Core.Bloat;

public class OfflineTweaks
{
    private readonly string _mountPath;

    public OfflineTweaks(string mountPath) => _mountPath = mountPath;

    public void Apply(IProgress<string>? progress = null)
    {
        using var hives = new HiveManager(_mountPath);
        hives.Load(progress);

        progress?.Report("Applying offline tweaks...");
        ApplyHardwareBypass(progress);
        ApplySponsoredApps(progress);
        ApplyPrivacyTelemetry(progress);
        ApplyMiscDebloat(progress);
        ApplyPreventReinstall(progress);

        hives.Unload(progress);
        progress?.Report("Offline tweaks complete.");
    }

    private static void ApplyHardwareBypass(IProgress<string>? p)
    {
        p?.Report("Bypassing hardware requirements...");
        // zDEFAULT
        Set(@"HKEY_LOCAL_MACHINE\zDEFAULT\Control Panel\UnsupportedHardwareNotificationCache", "SV1", 0);
        Set(@"HKEY_LOCAL_MACHINE\zDEFAULT\Control Panel\UnsupportedHardwareNotificationCache", "SV2", 0);
        // zNTUSER
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Control Panel\UnsupportedHardwareNotificationCache", "SV1", 0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Control Panel\UnsupportedHardwareNotificationCache", "SV2", 0);
        // LabConfig
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\LabConfig", "BypassCPUCheck",        1);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\LabConfig", "BypassRAMCheck",        1);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\LabConfig", "BypassSecureBootCheck", 1);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\LabConfig", "BypassStorageCheck",    1);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\LabConfig", "BypassTPMCheck",        1);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\Setup\MoSetup",   "AllowUpgradesWithUnsupportedTPMOrCPU", 1);
    }

    private static void ApplySponsoredApps(IProgress<string>? p)
    {
        p?.Report("Disabling sponsored/suggested apps...");
        const string cdm = @"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        Set(cdm, "OemPreInstalledAppsEnabled",        0);
        Set(cdm, "PreInstalledAppsEnabled",           0);
        Set(cdm, "SilentInstalledAppsEnabled",        0);
        Set(cdm, "ContentDeliveryAllowed",            0);
        Set(cdm, "FeatureManagementEnabled",          0);
        Set(cdm, "PreInstalledAppsEverEnabled",       0);
        Set(cdm, "SoftLandingEnabled",                0);
        Set(cdm, "SubscribedContentEnabled",          0);
        Set(cdm, "SubscribedContent-310093Enabled",   0);
        Set(cdm, "SubscribedContent-338388Enabled",   0);
        Set(cdm, "SubscribedContent-338389Enabled",   0);
        Set(cdm, "SubscribedContent-338393Enabled",   0);
        Set(cdm, "SubscribedContent-353694Enabled",   0);
        Set(cdm, "SubscribedContent-353696Enabled",   0);
        Set(cdm, "SystemPaneSuggestionsEnabled",      0);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures",    1);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableConsumerAccountStateContent", 1);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableCloudOptimizedContent",       1);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\PolicyManager\current\device\Start", "ConfigureStartPins",
            "{\"pinnedList\": [{}]}", RegistryValueKind.String);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\PushToInstall", "DisablePushToInstall", 1);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\MRT",           "DontOfferThroughWUAU", 1);
    }

    private static void ApplyPrivacyTelemetry(IProgress<string>? p)
    {
        p?.Report("Disabling telemetry and tracking...");
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",            "Enabled",                              0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Windows\CurrentVersion\Privacy",                    "TailoredExperiencesWithDiagnosticDataEnabled", 0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy",       "HasAccepted",                          0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Input\TIPC",                                        "Enabled",                              0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\InputPersonalization",                              "RestrictImplicitInkCollection",        1);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\InputPersonalization",                              "RestrictImplicitTextCollection",       1);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\InputPersonalization\TrainedDataStore",             "HarvestContacts",                      0);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\Software\Microsoft\Personalization\Settings",                          "AcceptedPrivacyPolicy",                0);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\DataCollection",                         "AllowTelemetry",                       0);
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\ControlSet001\Services\dmwappushservice",                              "Start",                                4);
    }

    private static void ApplyMiscDebloat(IProgress<string>? p)
    {
        p?.Report("Applying misc debloat settings...");
        // OOBE local account bypass
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\Windows\CurrentVersion\OOBE", "BypassNRO", 1);
        // Reserved storage
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves", 0);
        // BitLocker auto-encryption
        Set(@"HKEY_LOCAL_MACHINE\zSYSTEM\ControlSet001\Control\BitLocker", "PreventDeviceEncryption", 1);
        // Chat / Teams taskbar icon
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\Windows Chat", "ChatIcon", 3);
        Set(@"HKEY_LOCAL_MACHINE\zNTUSER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0);
        // OneDrive folder backup
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
        // Copilot
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Edge",                   "HubsSidebarEnabled",    0);
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\Explorer",       "DisableSearchBoxSuggestions", 1);
        // Teams installation prevention
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Teams", "DisableInstallation", 1);
        // New Outlook prevention
        Set(@"HKEY_LOCAL_MACHINE\zSOFTWARE\Policies\Microsoft\Windows\Windows Mail", "PreventRun", 1);
    }

    private static void ApplyPreventReinstall(IProgress<string>? p)
    {
        p?.Report("Preventing DevHome and Outlook reinstall...");
        const string orch = @"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Orchestrator\UScheduler_Oobe\OutlookUpdate";
        const string orch2 = @"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Orchestrator\UScheduler\OutlookUpdate";
        const string orch3 = @"HKEY_LOCAL_MACHINE\zSOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Orchestrator\UScheduler\DevHomeUpdate";
        Set(orch,  "workCompleted", 1);
        Set(orch2, "workCompleted", 1);
        Set(orch3, "workCompleted", 1);
    }

    private static void Set(string fullPath, string name, object value,
        RegistryValueKind kind = RegistryValueKind.DWord)
    {
        // fullPath is like HKEY_LOCAL_MACHINE\zSOFTWARE\...
        // Split off the root
        var slash = fullPath.IndexOf('\\');
        var root  = fullPath[..slash];
        var sub   = fullPath[(slash + 1)..];

        var hive = root switch
        {
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_CURRENT_USER"  => Registry.CurrentUser,
            _                    => throw new ArgumentException($"Unknown hive: {root}")
        };

        using var key = hive.CreateSubKey(sub, writable: true)
            ?? throw new InvalidOperationException($"Cannot open key: {fullPath}");
        key.SetValue(name, value, kind);
    }
}
