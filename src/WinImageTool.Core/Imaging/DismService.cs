using Microsoft.Dism;

namespace WinImageTool.Core.Imaging;

public sealed class DismService : IDisposable
{
    private string? _mountPath;
    private bool _isInitialized;
    private string? _scratchDir;

    public string? ScratchDir => _scratchDir;

    public void SetScratchDir(string? path)
    {
        _scratchDir = string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;
        DismApi.Initialize(DismLogLevel.LogErrors);
        _isInitialized = true;
    }

    public void MountImage(string wimPath, string mountPath, int imageIndex)
    {
        EnsureInitialized();
        Directory.CreateDirectory(mountPath);
        string? prevScratch = null;
        if (_scratchDir != null)
        {
            prevScratch = Environment.GetEnvironmentVariable("DISM_SCRATCH_DIR");
            Environment.SetEnvironmentVariable("DISM_SCRATCH_DIR", _scratchDir);
        }
        try
        {
            DismApi.MountImage(wimPath, mountPath, imageIndex, false);
            _mountPath = mountPath;
        }
        finally
        {
            if (_scratchDir != null && prevScratch != null)
                Environment.SetEnvironmentVariable("DISM_SCRATCH_DIR", prevScratch);
            else if (_scratchDir != null)
                Environment.SetEnvironmentVariable("DISM_SCRATCH_DIR", null);
        }
    }

    public void UnmountImage(bool commit)
    {
        EnsureInitialized();
        if (_mountPath == null) return;
        DismApi.UnmountImage(_mountPath, commit);
        _mountPath = null;
    }

    public IReadOnlyList<DismPackage> GetPackages(string mountPath)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        return [.. DismApi.GetPackages(session)];
    }

    public IReadOnlyList<DismFeature> GetFeatures(string mountPath)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        return [.. DismApi.GetFeatures(session)];
    }

    public void RemovePackageByName(string mountPath, string packageName,
        DismProgressCallback? progress = null)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        if (progress != null)
            DismApi.RemovePackageByName(session, packageName, progress);
        else
            DismApi.RemovePackageByName(session, packageName);
    }

    public void DisableFeature(string mountPath, string featureName,
        DismProgressCallback? progress = null)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        if (progress != null)
            DismApi.DisableFeature(session, featureName, null!, false, progress);
        else
            DismApi.DisableFeature(session, featureName, null!, false);
    }

    public void EnableFeature(string mountPath, string featureName,
        DismProgressCallback? progress = null)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        if (progress != null)
            DismApi.EnableFeature(session, featureName, true, false, null, progress);
        else
            DismApi.EnableFeature(session, featureName, true, false);
    }

    public void AddDriver(string mountPath, string driverPath, bool forceUnsigned = false)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        DismApi.AddDriver(session, driverPath, forceUnsigned);
    }

    public IReadOnlyList<DismDriverPackage> GetDrivers(string mountPath)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        return [.. DismApi.GetDrivers(session, false)];
    }

    public void AddPackage(string mountPath, string packagePath,
        DismProgressCallback? progress = null)
    {
        EnsureInitialized();
        using var session = DismApi.OpenOfflineSession(mountPath);
        if (progress != null)
            DismApi.AddPackage(session, packagePath, false, false, progress);
        else
            DismApi.AddPackage(session, packagePath, false, false);
    }

    public IReadOnlyList<DismMountedImageInfo> GetMountedImages()
    {
        EnsureInitialized();
        return [.. DismApi.GetMountedImages()];
    }

    public void Dispose()
    {
        if (_mountPath != null) UnmountImage(false);
        if (_isInitialized) DismApi.Shutdown();
    }
}
