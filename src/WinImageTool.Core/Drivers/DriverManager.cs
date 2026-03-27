using Microsoft.Dism;
using WinImageTool.Core.Imaging;

namespace WinImageTool.Core.Drivers;

public record DriverInfo(string PublishedName, string OriginalFileName, string ProviderName,
    string ClassName, string ClassGuid, string Version, string Date, bool BootCritical);

public class DriverManager
{
    private readonly DismService _dism;

    public DriverManager(DismService dism) => _dism = dism;

    public IReadOnlyList<DriverInfo> ListDrivers(string mountPath)
    {
        return _dism.GetDrivers(mountPath)
            .Select(d => new DriverInfo(
                d.PublishedName, d.OriginalFileName, d.ProviderName,
                d.ClassName, d.ClassGuid,
                d.Version.ToString(),
                d.Date.ToString("yyyy-MM-dd"),
                d.BootCritical))
            .OrderBy(d => d.ClassName)
            .ThenBy(d => d.PublishedName)
            .ToList();
    }

    public void AddDriver(string mountPath, string infPath, bool forceUnsigned = false,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Adding driver: {infPath}");
        _dism.AddDriver(mountPath, infPath, forceUnsigned);
        progress?.Report($"Driver added: {Path.GetFileName(infPath)}");
    }

    public void AddDriversFromFolder(string mountPath, string folder, bool recurse = true,
        bool forceUnsigned = false, IProgress<string>? progress = null)
    {
        var infFiles = Directory.GetFiles(folder, "*.inf",
            recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        foreach (var inf in infFiles)
            AddDriver(mountPath, inf, forceUnsigned, progress);
    }
}
