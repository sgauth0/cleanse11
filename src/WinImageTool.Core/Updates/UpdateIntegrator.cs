using Microsoft.Dism;
using WinImageTool.Core.Imaging;

namespace WinImageTool.Core.Updates;

public class UpdateIntegrator
{
    private static readonly string[] SupportedExtensions = [".msu", ".cab"];
    private readonly DismService _dism;

    public UpdateIntegrator(DismService dism) => _dism = dism;

    public void IntegrateUpdate(string mountPath, string packagePath,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Integrating: {Path.GetFileName(packagePath)}");
        DismProgressCallback? cb = progress == null ? null :
            p => progress.Report($"  {p.Current}/{p.Total}%");
        _dism.AddPackage(mountPath, packagePath, cb);
        progress?.Report($"Integrated: {Path.GetFileName(packagePath)}");
    }

    public void IntegrateUpdatesFromFolder(string mountPath, string folder,
        IProgress<string>? progress = null)
    {
        var packages = Directory
            .GetFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f)
            .ToList();

        progress?.Report($"Found {packages.Count} update(s) to integrate.");
        foreach (var pkg in packages)
            IntegrateUpdate(mountPath, pkg, progress);

        progress?.Report("Update integration complete.");
    }
}
