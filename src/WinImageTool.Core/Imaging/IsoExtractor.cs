using System.Diagnostics;

namespace WinImageTool.Core.Imaging;

public static class IsoExtractor
{
    public static bool IsIsoFile(string path)
        => path.EndsWith(".iso", StringComparison.OrdinalIgnoreCase);

    public static string ExtractWimFromIso(string isoPath, string? targetDir = null, IProgress<string>? progress = null)
    {
        var tempDir = targetDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinImageTool", "WorkingDir", "WimCache");
        Directory.CreateDirectory(tempDir);
        
        string? wimFile = null;

        try
        {
            progress?.Report("Mounting ISO...");

            var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"Mount-DiskImage -ImagePath '{isoPath}'\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var proc = Process.Start(psi))
                proc?.WaitForExit();

            System.Threading.Thread.Sleep(3000);

            string? sourceDrive = null;
            var drives = DriveInfo.GetDrives();
            
            foreach (var drive in drives)
            {
                if (drive.DriveType != DriveType.CDRom) continue;
                try
                {
                    if (!drive.IsReady) continue;
                    var wimTest = Path.Combine(drive.RootDirectory.FullName, "sources", "install.wim");
                    var esdTest = Path.Combine(drive.RootDirectory.FullName, "sources", "install.esd");
                    if (File.Exists(wimTest) || File.Exists(esdTest))
                    {
                        sourceDrive = drive.RootDirectory.FullName;
                        break;
                    }
                }
                catch { }
            }

            if (sourceDrive == null)
            {
                for (char c = 'D'; c <= 'Z'; c++)
                {
                    var testPath = $"{c}:\\";
                    if (!Directory.Exists(testPath)) continue;
                    try
                    {
                        var wimTest = Path.Combine(testPath, "sources", "install.wim");
                        var esdTest = Path.Combine(testPath, "sources", "install.esd");
                        if (File.Exists(wimTest) || File.Exists(esdTest))
                        {
                            sourceDrive = testPath;
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (sourceDrive == null)
                throw new InvalidOperationException("Could not find mounted ISO. Make sure you're running as Administrator.");

            progress?.Report($"ISO mounted at {sourceDrive}");

            var wimPath = Path.Combine(sourceDrive, "sources", "install.wim");
            var esdPath = Path.Combine(sourceDrive, "sources", "install.esd");

            string sourceWim;
            if (File.Exists(wimPath))
                sourceWim = wimPath;
            else if (File.Exists(esdPath))
                sourceWim = esdPath;
            else
                throw new FileNotFoundException("No install.wim or install.esd found in ISO.");

            var fileSize = new FileInfo(sourceWim).Length;
            var sizeGB = fileSize / 1024.0 / 1024.0 / 1024.0;
            
            var isoName = Path.GetFileNameWithoutExtension(isoPath);
            var wimSubDir = Path.Combine(tempDir, isoName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(wimSubDir);
            wimFile = Path.Combine(wimSubDir, Path.GetFileName(sourceWim));

            progress?.Report($"Copying {Path.GetFileName(sourceWim)} ({sizeGB:F2} GB) to {wimSubDir}...");

            File.Copy(sourceWim, wimFile, overwrite: true);
            
            File.SetAttributes(wimFile, FileAttributes.Normal);

            progress?.Report("Verifying file...");

            System.Threading.Thread.Sleep(1000);

            if (!File.Exists(wimFile))
                throw new IOException("File copy failed - file not found");

            var copiedSize = new FileInfo(wimFile).Length;
            if (copiedSize != fileSize)
                throw new IOException($"File copy incomplete ({copiedSize:N0} of {fileSize:N0} bytes)");

            progress?.Report("Dismounting ISO...");

            DismountIso(isoPath);

            progress?.Report($"WIM ready: {wimFile}");

            return wimFile;
        }
        catch (Exception ex)
        {
            try { DismountIso(isoPath); } catch { }
            throw new InvalidOperationException($"ISO extraction failed: {ex.Message}");
        }
    }

    private static void DismountIso(string isoPath)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"Dismount-DiskImage -ImagePath '{isoPath}'\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }
        catch { }
    }

    public static void CleanupCache(string? basePath = null)
    {
        try
        {
            var cachePath = basePath != null 
                ? Path.Combine(basePath, "Cleanse11_WimCache")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cleanse11_WimCache");
                
            if (Directory.Exists(cachePath))
            {
                foreach (var file in Directory.GetFiles(cachePath, "*.wim"))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch { }
                }
                foreach (var file in Directory.GetFiles(cachePath, "*.esd"))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
}
