using System.Diagnostics;

namespace WinImageTool.Core.Imaging;

public class IsoBuilder
{
    private static readonly string[] OscdimgSearchPaths =
    [
        @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
        @"C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
    ];

    private readonly string _oscdimgPath;

    public IsoBuilder()
    {
        _oscdimgPath = OscdimgSearchPaths.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "oscdimg.exe not found. Install the Windows ADK Deployment Tools.");
    }

    public static bool IsOscdimgAvailable()
        => OscdimgSearchPaths.Any(File.Exists);

    public void BuildIso(string sourceDirectory, string outputIsoPath,
        string volumeLabel = "WINDOWS", IProgress<string>? progress = null)
    {
        string etfsboot = Path.Combine(sourceDirectory, "boot", "etfsboot.com");
        string efisys = Path.Combine(sourceDirectory, "efi", "microsoft", "boot", "efisys.bin");

        bool hasBios = File.Exists(etfsboot);
        bool hasEfi = File.Exists(efisys);

        var args = new System.Text.StringBuilder();
        args.Append($"-m -o -u2 -udfver102 ");
        args.Append($"-l\"{volumeLabel}\" ");

        if (hasBios && hasEfi)
            args.Append($"-bootdata:2#p0,e,b\"{etfsboot}\"#pEF,e,b\"{efisys}\" ");
        else if (hasBios)
            args.Append($"-b\"{etfsboot}\" -pEF ");
        else if (hasEfi)
            args.Append($"-b\"{efisys}\" -pEF ");

        args.Append($"\"{sourceDirectory}\" \"{outputIsoPath}\"");

        progress?.Report($"Building ISO: {outputIsoPath}");

        var psi = new ProcessStartInfo(_oscdimgPath, args.ToString())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start oscdimg.exe");

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) progress?.Report(e.Data); };
        proc.BeginOutputReadLine();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            string err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"oscdimg failed (exit {proc.ExitCode}): {err}");
        }

        progress?.Report("ISO created successfully.");
    }
}
