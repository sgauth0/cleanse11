using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinImageTool.Core.Imaging;

public sealed class WimManager : IDisposable
{
    public WimFileInfo OpenWim(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        try
        {
            return OpenWimWithDism(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot read WIM file: {ex.Message}");
        }
    }

    private WimFileInfo OpenWimWithDism(string path)
    {
        var images = new List<WimImageInfo>();

        var psi = new ProcessStartInfo("dism.exe", $"/Get-WimInfo /WimFile:\"{path}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start DISM");
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"DISM failed: {err}");
        }

        var indexMatches = Regex.Matches(output, @"Index\s*:\s*(\d+)");
        var nameMatches = Regex.Matches(output, @"Name\s*:\s*(.+)");
        var descMatches = Regex.Matches(output, @"Description\s*:\s*(.+)");
        var archMatches = Regex.Matches(output, @"Architecture\s*:\s*(.+)");
        var sizeMatches = Regex.Matches(output, @"Size\s*:\s*([\d,]+)");

        var count = Math.Min(indexMatches.Count, nameMatches.Count);

        for (int i = 0; i < count; i++)
        {
            var img = new WimImageInfo
            {
                Index = int.Parse(indexMatches[i].Groups[1].Value),
                Name = i < nameMatches.Count ? nameMatches[i].Groups[1].Value.Trim() : $"Image {i + 1}",
                Description = i < descMatches.Count ? descMatches[i].Groups[1].Value.Trim() : "",
                Architecture = i < archMatches.Count ? archMatches[i].Groups[1].Value.Trim() : "",
                SizeBytes = i < sizeMatches.Count ? long.Parse(sizeMatches[i].Groups[1].Value.Replace(",", "")) : 0
            };
            images.Add(img);
        }

        if (images.Count == 0)
        {
            throw new InvalidOperationException("No images found in WIM file");
        }

        return new WimFileInfo { FilePath = path, Images = images };
    }

    public void Dispose() { }
}
