using Microsoft.Dism;
using WinImageTool.Core.Imaging;

namespace WinImageTool.Core.Components;

public enum RemoveSafety { Safe, Caution, Dangerous }

public record ComponentInfo(string Name, string State, string ReleaseType, RemoveSafety Safety);
public record FeatureInfo(string Name, string State, RemoveSafety Safety);

public static class SafetyClassifier
{
    private static readonly string[] SafePatterns = [
        "Language", "LanguagePack", "LangPack", "lp.cab",
        "RetailDemo", "ContentDeliveryManager",
        "EasySetup", "WebExperience",
        "Noto", "Font", "Ayu", "Morpheus",
        "NetFX", "WMI",
        "QuickAssist", "ScreenSketch", "StickyNotes",
        "Tablet", "TextInput", "OCR",
        "Wallet", "ExtAccess",
    ];

    private static readonly string[] CautionPatterns = [
        "MediaPlayer", "WindowsMediaPlayer", "WMP",
        "Xps", " XPSViewer",
        "WindowsPhoto", "Photo", "Camera", "Imaging",
        "VCLibs", "VC", "DesktopAppInstaller",
        "Hello", "Face", "Biometrics",
        "Edge", "WebView", "Webdriver",
        "Paint", "3D", "Sketchpad",
        "Teams", "Skype", "Discord", "Spotify",
        "Cocoa", "WhatsApp", "Telegram",
        "Netflix", "PrimeVideo", "Disney",
        "Office", "Word", "Excel", "PowerPoint",
        "OneNote", "Outlook", "Publisher",
        "DesktopBridge", "UWP", "UWPCore",
    ];

    private static readonly string[] DangerousPatterns = [
        "Hyper", "Hypervisor", "Virtual", "Vm",
        "BitLocker", "Bitlocker",
        "SecureBoot", "UEFI", "TPM",
        "Boot", "WinRE", "Recovery",
        "Networking", "Network", "NearField", "NFC",
        "Bluetooth",
        "Wireless", "Wi-Fi", "WLAN",
        "Graphics", "Display", "GPU", "VGA", "IntelGPU",
        "Audio", "Sound", "Speaker", "Microphone", "Codec",
        "Touch", "Pen", "Dial",
        "USB", "Storage", "Disk", "AHCI", "RAID", "SATA",
        "Intel", "AMD", "NVIDIA", "Qualcomm",
        "Ethernet", "LAN", "NIC",
        "Cam", "Infrared",
        "Sensor", "Accelerometer", "LightSensor",
        "Telemetry", "Diag", "Diagnostics",
        "Update", "Servicing", "Setup",
        "WinDefender", "Defender", "Antimalware", "Security",
        "OneDrive", "SkyDrive",
        "Shell", "Explorer", "StartMenu", "StartMenu",
        "Desktop", "Taskbar", "Search", "Cortana",
        "Input", "Keyboard", "Mouse", "Pointing",
        "Print", "PrintToPDF", "PDF", "XPS",
        "Fax", "Scan", "FaxScan",
        "FileHistory", "Backup", "SystemRestore",
        "Narrator", "Speech", "Voice", "TTS",
        "Accessibility", "Magnifier", "OnScreenKeyboard",
        "RuntimeBroker", "AppResolver",
        "DirectX", "DX", "Direct3D", "DirectWrite",
        ".NET", "dotnet", "Netfx",
        "PowerShell", "PSModule", "PSReadline",
        "WinRM", "Remoting", "SSH",
        "SMB", "FileShare", "NetworkDrive",
        "ISCSI", "iSCSI",
        "VPN", "RasMgmt", "RemoteAccess",
        "IPHelper", "Tcpip", "DNS", "DHCP",
        "Firewall", "WF",
        "EmbeddedMode", "IoT", "Embedded",
    ];

    public static RemoveSafety Classify(string name, string releaseType)
    {
        var lower = name.ToLowerInvariant();

        foreach (var p in DangerousPatterns)
            if (lower.Contains(p.ToLowerInvariant())) return RemoveSafety.Dangerous;

        foreach (var p in CautionPatterns)
            if (lower.Contains(p.ToLowerInvariant())) return RemoveSafety.Caution;

        foreach (var p in SafePatterns)
            if (lower.Contains(p.ToLowerInvariant())) return RemoveSafety.Safe;

        if (releaseType.Contains("Language", StringComparison.OrdinalIgnoreCase))
            return RemoveSafety.Safe;
        if (releaseType.Contains("Patch", StringComparison.OrdinalIgnoreCase) ||
            releaseType.Contains("Update", StringComparison.OrdinalIgnoreCase))
            return RemoveSafety.Caution;

        return RemoveSafety.Caution;
    }
}

public class ComponentManager
{
    private readonly DismService _dism;

    public ComponentManager(DismService dism) => _dism = dism;

    public IReadOnlyList<ComponentInfo> ListComponents(string mountPath)
    {
        return _dism.GetPackages(mountPath)
            .Select(p => {
                var safety = SafetyClassifier.Classify(p.PackageName, p.ReleaseType.ToString());
                return new ComponentInfo(p.PackageName, p.PackageState.ToString(), p.ReleaseType.ToString(), safety);
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public IReadOnlyList<FeatureInfo> ListFeatures(string mountPath)
    {
        return _dism.GetFeatures(mountPath)
            .Select(f => {
                var safety = SafetyClassifier.Classify(f.FeatureName, "");
                return new FeatureInfo(f.FeatureName, f.State.ToString(), safety);
            })
            .OrderBy(f => f.Name)
            .ToList();
    }

    public void RemovePackage(string mountPath, string packageName,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Removing: {packageName}");
        DismProgressCallback? cb = progress == null ? null :
            p => progress.Report($"  {p.Current}/{p.Total}%");
        _dism.RemovePackageByName(mountPath, packageName, cb);
        progress?.Report($"Removed: {packageName}");
    }

    public void DisableFeature(string mountPath, string featureName,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Disabling: {featureName}");
        DismProgressCallback? cb = progress == null ? null :
            p => progress.Report($"  {p.Current}/{p.Total}%");
        _dism.DisableFeature(mountPath, featureName, cb);
        progress?.Report($"Disabled: {featureName}");
    }
}

public enum FullPreset { Lite, OpenClaw }

public record ComponentPreset(string Name, string Description, string[] MatchPatterns);

public static class ComponentPresets
{
    public static readonly ComponentPreset[] All = [
        new("Lite",        "Aggressively remove bloatware and consumer apps. Good for low-end PCs or minimal installs.", [
            "RetailDemo", "ContentDeliveryManager", "CommBindEngine", "DevHome",
            "Facebook", "Instagram", "Spotify", "TikTok", "Twitter", "WhatsApp",
            "Disney", "Hulu", "PrimeVideo", "Netflix", "Paramount", "Peacock",
            "Teams", "Skype", "Discord", "Telegram", "Slack",
            "Office", "Word", "Excel", "PowerPoint", "Outlook", "Publisher", "OneNote",
            "OneDrive", "SkyDrive",
            "MediaPlayer", "WMP", "WMA", "Zune",
            "WindowsPhoto", "Photos", "Camera", "PhotosApp",
            "Paint", "MSPaint", "Paint3D", "Sketchpad", "3DBuilder",
            "VCLibs", "VC", "DesktopAppInstaller", "AppInstaller",
            "Xbox", "GameBar", "Gaming", "XboxIdentity",
            "Bing", "SearchHost", "Cortana", "WebExperience",
            "Clipchamp", "MixedReality", "Portali", "Teams",
            "ECO", "EcoQA", "FeedbackHub",
            "Messaging", "Messages", "MyPhone", "Phone", "Calls",
            "Maps", "MapControl", "StreetsAndTrips",
            "Weather", "WeatherService",
            "News", "NewsStand", "Newsroom",
            "Sports", "SportsApp",
            "Finance", "FinanceApp",
            "Solitaire", "Casino", "Game", "Entertainment",
            "GetHelp", "HelpAndTips", "Tips", "GetStarted", "Start",
            "Noto", "Font", "Ayu", "Morpheus", "Emoji", "SegoeUI",
            "Language", "LanguagePack", "LangPack",
            "QuickAssist", "ScreenSketch", "StickyNotes", "Notepad",
            "Wallet", "ExtAccess", "ExtDeploy",
        ]),
        new("Dev Environment", "Development-focused: keeps developer features, removes consumer bloat.", [
            "RetailDemo", "ContentDeliveryManager", "CommBindEngine", "DevHome",
            "Facebook", "Instagram", "Spotify", "TikTok", "Twitter", "WhatsApp",
            "Disney", "Hulu", "PrimeVideo", "Netflix", "Paramount", "Peacock",
            "Teams", "Skype", "Discord", "Telegram",
            "Office", "Word", "Excel", "PowerPoint", "Outlook", "Publisher",
            "OneDrive", "SkyDrive",
            "MediaPlayer", "WMP", "WMA", "Zune",
            "WindowsPhoto", "Photos", "Camera",
            "Paint", "MSPaint", "Paint3D", "Sketchpad",
            "Xbox", "GameBar", "Gaming", "XboxIdentity",
            "Bing", "Cortana", "WebExperience",
            "Clipchamp", "MixedReality", "Teams",
            "ECO", "EcoQA", "FeedbackHub",
            "Messaging", "Messages", "MyPhone", "Phone", "Calls",
            "Maps", "MapControl", "StreetsAndTrips",
            "Weather", "WeatherService",
            "News", "NewsStand", "Newsroom",
            "Sports", "SportsApp",
            "Finance", "FinanceApp",
            "Solitaire", "Casino", "Game", "Entertainment",
            "GetHelp", "HelpAndTips", "Tips", "GetStarted",
            "Wallet", "ExtAccess", "ExtDeploy",
        ]),
        new("Openclaw", "Optimized for running OpenClaw AI assistant. Keeps dev tools and runtime dependencies, removes consumer bloat.", [
            "RetailDemo", "ContentDeliveryManager", "CommBindEngine", "DevHome",
            "Facebook", "Instagram", "Spotify", "TikTok", "Twitter", "WhatsApp",
            "Disney", "Hulu", "Netflix", "Paramount", "Peacock",
            "Skype", "Telegram", "Slack",
            "Office", "Word", "Excel", "PowerPoint", "Outlook",
            "OneDrive", "SkyDrive",
            "MediaPlayer", "WMP", "WMA", "Zune",
            "WindowsPhoto", "Photos", "Camera",
            "Paint", "MSPaint", "Paint3D", "Sketchpad",
            "VCLibs", "VC", "DesktopAppInstaller",
            "Bing", "Cortana", "WebExperience",
            "Clipchamp", "MixedReality",
            "ECO", "EcoQA", "FeedbackHub",
            "Messaging", "Messages", "MyPhone", "Phone", "Calls",
            "Maps", "MapControl", "StreetsAndTrips",
            "Weather", "WeatherService",
            "News", "NewsStand", "Newsroom",
            "Sports", "SportsApp",
            "Finance", "FinanceApp",
            "Solitaire", "Casino", "Entertainment",
            "GetHelp", "HelpAndTips", "Tips", "GetStarted",
            "QuickAssist", "ScreenSketch", "StickyNotes",
            "Wallet", "ExtAccess", "ExtDeploy",
        ]),
        new("Security / Privacy", "Removes telemetry, tracking, and privacy-invading components.", [
            "RetailDemo", "ContentDeliveryManager",
            "Telemetry", "Diag", "Diagnostics", "DiagTrack", "ULL",
            "WMPNetwork", "WMP", "Zune",
            "BitLocker", "Bitlocker", "Boot", "WinRE",
            "Defender", "WinDefender", "Antimalware", "Security",
            "Edge", "WebView", "Webdriver",
            "OneDrive", "SkyDrive",
            "FeedbackHub", "Feedback", "FeedbackService",
            "CEIP", "CEIPData", "TelemetryData",
            "Wallet", "ExtAccess",
        ]),
    ];
}
