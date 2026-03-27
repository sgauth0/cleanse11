using System.Xml.Serialization;

namespace WinImageTool.Core.Settings;

[XmlRoot("WinImageToolSettings")]
public class AppSettings
{
    public string UpdatesCacheDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinImageTool", "Cache", "Updates");

    public string ImageScratchDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinImageTool", "Cache", "Images");

    public string WorkingDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinImageTool", "WorkingDir");

    public List<string> RecentMountPaths { get; set; } = [];

    public bool ForceUnsignedDrivers { get; set; } = false;
    public bool ForceWimLib { get; set; } = false;
    public int WimCompression { get; set; } = 2;
    public bool VerifyImageFiles { get; set; } = true;
    public bool ShowExtraImageInfo { get; set; } = true;
    public string VisualTheme { get; set; } = "System";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinImageTool", "settings.xml");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try
        {
            using var stream = File.OpenRead(SettingsPath);
            var xs = new XmlSerializer(typeof(AppSettings));
            return (AppSettings?)xs.Deserialize(stream) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        using var stream = File.Create(SettingsPath);
        var xs = new XmlSerializer(typeof(AppSettings));
        xs.Serialize(stream, this);
    }

    public void AddRecentMountPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentMountPaths.Remove(path);
        RecentMountPaths.Insert(0, path);
        if (RecentMountPaths.Count > 10)
            RecentMountPaths.RemoveAt(RecentMountPaths.Count - 1);
    }
}
