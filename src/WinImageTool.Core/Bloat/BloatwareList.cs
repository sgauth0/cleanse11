// Bloatware package list sourced from tiny11builder (MIT-compatible, open source)
// https://github.com/ntdevlabs/tiny11builder

namespace WinImageTool.Core.Bloat;

public enum BloatCategory { AI, Communication, Gaming, Media, Microsoft365, Privacy, System }

public record BloatPackage(string Prefix, string DisplayName, BloatCategory Category, bool DefaultSelected = true);

public static class BloatwareList
{
    public static readonly IReadOnlyList<BloatPackage> All =
    [
        // AI / Copilot
        new("Microsoft.Copilot",                     "Copilot",                      BloatCategory.AI),
        new("Microsoft.Windows.Copilot",             "Windows Copilot",              BloatCategory.AI),
        new("Microsoft.BingSearch",                  "Bing Search",                  BloatCategory.AI),

        // Communication
        new("Microsoft.YourPhone",                   "Phone Link",                   BloatCategory.Communication),
        new("MicrosoftCorporationII.MicrosoftFamily","Microsoft Family",             BloatCategory.Communication),
        new("MSTeams",                               "Teams (personal)",             BloatCategory.Communication),
        new("MicrosoftTeams",                        "Teams",                        BloatCategory.Communication),
        new("Microsoft.Windows.Teams",               "Teams (built-in)",             BloatCategory.Communication),
        new("Microsoft.SkypeApp",                    "Skype",                        BloatCategory.Communication),
        new("microsoft.windowscommunicationsapps",   "Mail and Calendar",            BloatCategory.Communication),
        new("Microsoft.OutlookForWindows",           "New Outlook",                  BloatCategory.Communication),

        // Gaming
        new("Microsoft.GamingApp",                   "Xbox Gaming App",              BloatCategory.Gaming),
        new("Microsoft.Xbox.TCUI",                   "Xbox TCUI",                    BloatCategory.Gaming),
        new("Microsoft.XboxApp",                     "Xbox Console Companion",       BloatCategory.Gaming),
        new("Microsoft.XboxGameOverlay",             "Xbox Game Overlay",            BloatCategory.Gaming),
        new("Microsoft.XboxGamingOverlay",           "Xbox Game Bar",                BloatCategory.Gaming),
        new("Microsoft.XboxIdentityProvider",        "Xbox Identity Provider",       BloatCategory.Gaming),
        new("Microsoft.XboxSpeechToTextOverlay",     "Xbox Speech Overlay",          BloatCategory.Gaming),
        new("Microsoft.MicrosoftSolitaireCollection","Solitaire",                    BloatCategory.Gaming),

        // Media
        new("Clipchamp.Clipchamp",                   "Clipchamp",                    BloatCategory.Media),
        new("Microsoft.ZuneMusic",                   "Media Player",                 BloatCategory.Media),
        new("Microsoft.ZuneVideo",                   "Movies & TV",                  BloatCategory.Media),
        new("Microsoft.WindowsCamera",               "Camera",                       BloatCategory.Media),
        new("Microsoft.WindowsSoundRecorder",        "Sound Recorder",               BloatCategory.Media),
        new("Microsoft.MSPaint",                     "Paint (legacy)",               BloatCategory.Media),
        new("Microsoft.Microsoft3DViewer",           "3D Viewer",                    BloatCategory.Media),
        new("DolbyLaboratories.DolbyAccess",         "Dolby Access",                 BloatCategory.Media),
        new("DolbyLaboratories.DolbyDigitalPlusDecoderOEM", "Dolby Decoder",        BloatCategory.Media),

        // Microsoft 365 / Office
        new("Microsoft.MicrosoftOfficeHub",          "Office Hub",                   BloatCategory.Microsoft365),
        new("Microsoft.Office.OneNote",              "OneNote (UWP)",                BloatCategory.Microsoft365),
        new("Microsoft.OfficePushNotificationUtility","Office Push Notifications",   BloatCategory.Microsoft365),

        // Privacy / Telemetry
        new("Microsoft.BingNews",                    "Bing News",                    BloatCategory.Privacy),
        new("Microsoft.BingWeather",                 "Weather",                      BloatCategory.Privacy),
        new("Microsoft.WindowsFeedbackHub",          "Feedback Hub",                 BloatCategory.Privacy),
        new("Microsoft.549981C3F5F10",               "Cortana",                      BloatCategory.Privacy),

        // System / Misc
        new("Microsoft.GetHelp",                     "Get Help",                     BloatCategory.System),
        new("Microsoft.Getstarted",                  "Tips / Get Started",           BloatCategory.System),
        new("Microsoft.People",                      "People",                       BloatCategory.System),
        new("Microsoft.PowerAutomateDesktop",        "Power Automate",               BloatCategory.System),
        new("Microsoft.Todos",                       "Microsoft To Do",              BloatCategory.System),
        new("Microsoft.WindowsAlarms",               "Alarms & Clock",               BloatCategory.System),
        new("Microsoft.WindowsMaps",                 "Maps",                         BloatCategory.System),
        new("Microsoft.MicrosoftStickyNotes",        "Sticky Notes",                 BloatCategory.System),
        new("Microsoft.MixedReality.Portal",         "Mixed Reality Portal",         BloatCategory.System),
        new("Microsoft.Wallet",                      "Microsoft Wallet",             BloatCategory.System),
        new("Microsoft.Windows.DevHome",             "Dev Home",                     BloatCategory.System),
        new("Microsoft.Windows.CrossDevice",         "Cross Device Experience",      BloatCategory.System),
        new("Microsoft.StartExperiencesApp",         "Start Experiences",            BloatCategory.System),
        new("MicrosoftCorporationII.QuickAssist",    "Quick Assist",                 BloatCategory.System),
        new("Microsoft.WindowsTerminal",             "Windows Terminal",             BloatCategory.System, DefaultSelected: false),
        new("Microsoft.Paint",                       "Paint",                        BloatCategory.System, DefaultSelected: false),
        new("AppUp.IntelManagementandSecurityStatus","Intel ME Status",              BloatCategory.System),
    ];
}
