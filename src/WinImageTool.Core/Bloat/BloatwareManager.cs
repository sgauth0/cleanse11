using System.Diagnostics;

namespace WinImageTool.Core.Bloat;

public class BloatwareManager
{
    /// <summary>
    /// Returns the list of provisioned AppX package names in the mounted image
    /// that match any of the given bloat prefixes.
    /// </summary>
    public IReadOnlyList<string> FindBloatPackages(string mountPath,
        IEnumerable<BloatPackage> toRemove)
    {
        var provisioned = GetProvisionedPackages(mountPath);
        var prefixes = toRemove.Select(b => b.Prefix).ToList();

        return provisioned
            .Where(pkg => prefixes.Any(prefix =>
                pkg.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Removes provisioned AppX packages from a mounted image using DISM.exe
    /// (matches tiny11builder's approach exactly).
    /// </summary>
    public void RemovePackages(string mountPath, IEnumerable<BloatPackage> toRemove,
        IProgress<string>? progress = null)
    {
        var packages = FindBloatPackages(mountPath, toRemove);
        progress?.Report($"Found {packages.Count} bloatware package(s) to remove.");

        foreach (var pkg in packages)
        {
            progress?.Report($"Removing: {pkg}");
            RunDism(mountPath, "/Remove-ProvisionedAppxPackage", $"/PackageName:{pkg}");
            progress?.Report($"  Done.");
        }

        progress?.Report("Package removal complete.");
    }

    /// <summary>
    /// Removes Edge, OneDrive setup, and WebView directories from the mounted image.
    /// </summary>
    public void RemoveEdgeAndOneDrive(string mountPath, IProgress<string>? progress = null)
    {
        var dirs = new[]
        {
            Path.Combine(mountPath, "Program Files (x86)", "Microsoft", "Edge"),
            Path.Combine(mountPath, "Program Files (x86)", "Microsoft", "EdgeUpdate"),
            Path.Combine(mountPath, "Program Files (x86)", "Microsoft", "EdgeCore"),
            Path.Combine(mountPath, "Windows", "System32", "Microsoft-Edge-Webview"),
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            progress?.Report($"Removing: {dir}");
            TakeOwnership(dir, recursive: true);
            Directory.Delete(dir, recursive: true);
        }

        var oneDriveSetup = Path.Combine(mountPath, "Windows", "System32", "OneDriveSetup.exe");
        if (File.Exists(oneDriveSetup))
        {
            progress?.Report("Removing OneDriveSetup.exe");
            TakeOwnership(oneDriveSetup);
            File.Delete(oneDriveSetup);
        }

        progress?.Report("Edge and OneDrive removed.");
    }

    /// <summary>
    /// Removes telemetry-related scheduled task definition files from the mounted image.
    /// </summary>
    public void RemoveScheduledTasks(string mountPath, IProgress<string>? progress = null)
    {
        var tasksRoot = Path.Combine(mountPath, "Windows", "System32", "Tasks");
        var toRemove  = new[]
        {
            Path.Combine(tasksRoot, "Microsoft", "Windows", "Application Experience", "Microsoft Compatibility Appraiser"),
            Path.Combine(tasksRoot, "Microsoft", "Windows", "Application Experience", "ProgramDataUpdater"),
            Path.Combine(tasksRoot, "Microsoft", "Windows", "Customer Experience Improvement Program"),
            Path.Combine(tasksRoot, "Microsoft", "Windows", "Chkdsk",                "Proxy"),
            Path.Combine(tasksRoot, "Microsoft", "Windows", "Windows Error Reporting","QueueReporting"),
        };

        foreach (var path in toRemove)
        {
            if (File.Exists(path))      { File.Delete(path);                     progress?.Report($"Removed task file: {Path.GetFileName(path)}"); }
            if (Directory.Exists(path)) { Directory.Delete(path, recursive:true); progress?.Report($"Removed task folder: {Path.GetFileName(path)}"); }
        }
    }

    private IReadOnlyList<string> GetProvisionedPackages(string mountPath)
    {
        var psi = new ProcessStartInfo("dism",
            $"/English /image:\"{mountPath}\" /Get-ProvisionedAppxPackages")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        var packages = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("PackageName : ", StringComparison.OrdinalIgnoreCase))
                packages.Add(trimmed["PackageName : ".Length..].Trim());
        }
        return packages;
    }

    private static void RunDism(string mountPath, string operation, string argument)
    {
        var psi = new ProcessStartInfo("dism",
            $"/English /image:\"{mountPath}\" {operation} {argument}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
    }

    private static void TakeOwnership(string path, bool recursive = false)
    {
        var args = recursive ? $"/f \"{path}\" /r" : $"/f \"{path}\"";
        Run("takeown", args);
        var icaclsArgs = recursive
            ? $"\"{path}\" /grant Administrators:(F) /T /C"
            : $"\"{path}\" /grant Administrators:(F)";
        Run("icacls", icaclsArgs);
    }

    private static void Run(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            CreateNoWindow  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
    }
}
