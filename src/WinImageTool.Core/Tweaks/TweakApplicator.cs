using Microsoft.Win32;

namespace WinImageTool.Core.Tweaks;

/// <summary>
/// Applies tweaks to the live running system or an offline registry hive loaded at a custom path.
/// </summary>
public class TweakApplicator
{
    private readonly string? _hiveMountPoint;

    /// <param name="hiveMountPoint">
    /// Null = live system. When set, registry paths are prefixed with this hive mount point
    /// (e.g. "HKLM\WIT_OFFLINE_SYSTEM" after loading the offline SOFTWARE/SYSTEM hive).
    /// </param>
    public TweakApplicator(string? hiveMountPoint = null)
    {
        _hiveMountPoint = hiveMountPoint;
    }

    public void Apply(IEnumerable<TweakGroup> groups, IProgress<string>? progress = null)
    {
        foreach (var group in groups)
        {
            progress?.Report($"Applying: {group.DisplayName}");
            foreach (var entry in group.Entries)
                ApplyEntry(entry, progress);
        }
    }

    public void Apply(TweakGroup group, IProgress<string>? progress = null)
        => Apply([group], progress);

    public void ApplyAll(IProgress<string>? progress = null)
        => Apply(TweakCatalog.All, progress);

    public void ApplyEntry(TweakEntry entry, IProgress<string>? progress)
    {
        try
        {
            using var key = OpenOrCreate(entry);
            var value = ConvertValue(entry);
            var kind = ToRegistryValueKind(entry.Kind);
            key.SetValue(entry.Name, value, kind);
            progress?.Report($"  [OK] {entry.Description}");
        }
        catch (Exception ex)
        {
            progress?.Report($"  [WARN] {entry.Name}: {ex.Message}");
        }
    }

    private RegistryKey OpenOrCreate(TweakEntry entry)
    {
        string path = entry.Path;

        if (_hiveMountPoint != null)
        {
            // For offline hives, use the mounted point instead of the real hive root
            path = $"{_hiveMountPoint}\\{path}";
            return Registry.LocalMachine.CreateSubKey(
                path.Replace("HKLM\\", "").Replace("HKCU\\", ""),
                writable: true)
                ?? throw new InvalidOperationException($"Cannot open offline hive key: {path}");
        }

        var root = entry.Hive == RegistryHive.LocalMachine
            ? Registry.LocalMachine
            : Registry.CurrentUser;

        return root.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"Cannot create registry key: {path}");
    }

    private static object ConvertValue(TweakEntry entry) => entry.Kind switch
    {
        RegistryValueKind.DWord => Convert.ToInt32(entry.Value),
        RegistryValueKind.QWord => Convert.ToInt64(entry.Value),
        _ => entry.Value.ToString()!
    };

    private static Microsoft.Win32.RegistryValueKind ToRegistryValueKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.DWord       => Microsoft.Win32.RegistryValueKind.DWord,
        RegistryValueKind.QWord       => Microsoft.Win32.RegistryValueKind.QWord,
        RegistryValueKind.ExpandString => Microsoft.Win32.RegistryValueKind.ExpandString,
        _                             => Microsoft.Win32.RegistryValueKind.String
    };
}
