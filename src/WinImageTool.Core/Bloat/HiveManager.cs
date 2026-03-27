using System.Diagnostics;

namespace WinImageTool.Core.Bloat;

/// <summary>
/// Loads and unloads offline registry hives from a mounted Windows image.
/// Uses the same mount-point naming scheme as tiny11builder (zSOFTWARE, zSYSTEM, etc.)
/// </summary>
public sealed class HiveManager : IDisposable
{
    private readonly string _mountPath;
    private bool _loaded;

    public const string SoftwareMount = @"HKLM\zSOFTWARE";
    public const string SystemMount   = @"HKLM\zSYSTEM";
    public const string DefaultMount  = @"HKLM\zDEFAULT";
    public const string NtUserMount   = @"HKLM\zNTUSER";

    public HiveManager(string mountPath) => _mountPath = mountPath;

    public void Load(IProgress<string>? progress = null)
    {
        progress?.Report("Loading offline registry hives...");
        Reg("load", SoftwareMount, Path.Combine(_mountPath, "Windows", "System32", "config", "SOFTWARE"));
        Reg("load", SystemMount,   Path.Combine(_mountPath, "Windows", "System32", "config", "SYSTEM"));
        Reg("load", DefaultMount,  Path.Combine(_mountPath, "Windows", "System32", "config", "default"));
        Reg("load", NtUserMount,   Path.Combine(_mountPath, "Users",   "Default",  "NTUSER.DAT"));
        _loaded = true;
        progress?.Report("Hives loaded.");
    }

    public void Unload(IProgress<string>? progress = null)
    {
        if (!_loaded) return;
        progress?.Report("Unloading registry hives...");
        foreach (var hive in new[] { SoftwareMount, SystemMount, DefaultMount, NtUserMount })
        {
            try { Reg("unload", hive); }
            catch { /* best effort */ }
        }
        _loaded = false;
        progress?.Report("Hives unloaded.");
    }

    private static void Reg(string command, string hive, string? hivePath = null)
    {
        var args = hivePath != null ? $"{command} \"{hive}\" \"{hivePath}\"" : $"{command} \"{hive}\"";
        var psi = new ProcessStartInfo("reg", args)
        {
            UseShellExecute = false,
            CreateNoWindow  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"reg {command} {hive} failed: {err}");
        }
    }

    public void Dispose() => Unload();
}
