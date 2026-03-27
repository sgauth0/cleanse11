using CommandLine;
using WinImageTool.Core.Imaging;
using WinImageTool.Core.Components;
using WinImageTool.Core.Drivers;
using WinImageTool.Core.Updates;
using WinImageTool.Core.Tweaks;
using WinImageTool.Core.Unattended;

[Verb("info", HelpText = "List images inside a WIM/ESD/ISO file.")]
class InfoOptions
{
    [Value(0, Required = true, MetaName = "image", HelpText = "Path to WIM/ESD file.")] public string ImagePath { get; set; } = string.Empty;
}

[Verb("mount", HelpText = "Mount a WIM image for editing.")]
class MountOptions
{
    [Option('i', "image",   Required = true,  HelpText = "WIM file path.")]     public string ImagePath  { get; set; } = string.Empty;
    [Option('m', "mount",   Required = true,  HelpText = "Mount directory.")]   public string MountPath  { get; set; } = string.Empty;
    [Option('n', "index",   Default  = 1,     HelpText = "Image index.")]        public int    Index      { get; set; }
}

[Verb("unmount", HelpText = "Unmount a WIM image, optionally saving changes.")]
class UnmountOptions
{
    [Option('m', "mount",  Required = true, HelpText = "Mount directory.")] public string MountPath { get; set; } = string.Empty;
    [Option('c', "commit", Default  = false, HelpText = "Save changes.")]   public bool   Commit    { get; set; }
}

[Verb("components", HelpText = "List packages and features in a mounted image.")]
class ComponentsOptions
{
    [Option('m', "mount", Required = true, HelpText = "Mount directory.")] public string MountPath { get; set; } = string.Empty;
    [Option('f', "features", Default = false, HelpText = "List features instead of packages.")] public bool Features { get; set; }
}

[Verb("remove-package", HelpText = "Remove a Windows package from a mounted image.")]
class RemovePackageOptions
{
    [Option('m', "mount",   Required = true, HelpText = "Mount directory.")]  public string MountPath   { get; set; } = string.Empty;
    [Option('p', "package", Required = true, HelpText = "Package name.")]     public string PackageName { get; set; } = string.Empty;
}

[Verb("add-driver", HelpText = "Add a driver to a mounted image.")]
class AddDriverOptions
{
    [Option('m', "mount",  Required = true, HelpText = "Mount directory.")] public string MountPath  { get; set; } = string.Empty;
    [Option('d', "driver", Required = true, HelpText = "INF file or folder.")] public string DriverPath { get; set; } = string.Empty;
    [Option('u', "unsigned", Default = false, HelpText = "Allow unsigned drivers.")] public bool Unsigned { get; set; }
}

[Verb("add-update", HelpText = "Integrate update packages (.msu/.cab) into a mounted image.")]
class AddUpdateOptions
{
    [Option('m', "mount",  Required = true, HelpText = "Mount directory.")]         public string MountPath   { get; set; } = string.Empty;
    [Option('p', "path",   Required = true, HelpText = "Update file or folder.")]   public string PackagePath { get; set; } = string.Empty;
}

[Verb("tweak", HelpText = "Apply performance tweaks to the live system or a mounted image.")]
class TweakOptions
{
    [Option('a', "all",      Default = false, HelpText = "Apply all tweak categories.")] public bool All { get; set; }
    [Option('c', "category", HelpText = "Comma-separated categories (latency,input,ssd,gpu,network,cpu,power,responsiveness,boot,maintenance,ui,memory,directx,nvidia,amd).")]
    public string Categories { get; set; } = string.Empty;
    [Option('l', "list",     Default = false, HelpText = "List available categories.")] public bool List { get; set; }
}

[Verb("unattended", HelpText = "Generate an autounattend.xml file.")]
class UnattendedOptions
{
    [Option('o', "output",   Required = true,  HelpText = "Output XML path.")]        public string Output       { get; set; } = string.Empty;
    [Option('n', "name",     Default  = "",    HelpText = "Computer name.")]          public string ComputerName { get; set; } = string.Empty;
    [Option('t', "timezone", Default  = "UTC", HelpText = "Time zone.")]              public string TimeZone     { get; set; } = string.Empty;
    [Option('l', "lang",     Default  = "en-US", HelpText = "UI language.")]          public string Language     { get; set; } = string.Empty;
    [Option("skip-oobe",     Default  = true,  HelpText = "Skip OOBE screens.")]      public bool   SkipOobe     { get; set; }
    [Option("gpt",           Default  = true,  HelpText = "Use GPT disk layout.")]    public bool   Gpt          { get; set; }
}

class Program
{
    static int Main(string[] args)
    {
        return Parser.Default
            .ParseArguments<InfoOptions, MountOptions, UnmountOptions,
                            ComponentsOptions, RemovePackageOptions,
                            AddDriverOptions, AddUpdateOptions,
                            TweakOptions, UnattendedOptions>(args)
            .MapResult(
                (InfoOptions o)          => RunInfo(o),
                (MountOptions o)         => RunMount(o),
                (UnmountOptions o)       => RunUnmount(o),
                (ComponentsOptions o)    => RunComponents(o),
                (RemovePackageOptions o) => RunRemovePackage(o),
                (AddDriverOptions o)     => RunAddDriver(o),
                (AddUpdateOptions o)     => RunAddUpdate(o),
                (TweakOptions o)         => RunTweak(o),
                (UnattendedOptions o)    => RunUnattended(o),
                errs                     => 1);
    }

    static int RunInfo(InfoOptions o)
    {
        using var wim = new WimManager();
        var info = wim.OpenWim(o.ImagePath);
        Console.WriteLine($"File: {info.FilePath}");
        Console.WriteLine($"Images: {info.Images.Count}");
        Console.WriteLine();
        foreach (var img in info.Images)
        {
            Console.WriteLine($"  [{img.Index}] {img.Name}");
            if (!string.IsNullOrEmpty(img.Edition))     Console.WriteLine($"       Edition : {img.Edition}");
            if (!string.IsNullOrEmpty(img.Architecture)) Console.WriteLine($"       Arch    : {img.Architecture}");
            if (!string.IsNullOrEmpty(img.Version))      Console.WriteLine($"       Version : {img.Version}");
            Console.WriteLine($"       Size    : {img.SizeFormatted}");
        }
        return 0;
    }

    static int RunMount(MountOptions o)
    {
        using var dism = new DismService();
        dism.MountImage(o.ImagePath, o.MountPath, o.Index);
        Console.WriteLine($"Mounted index {o.Index} to {o.MountPath}");
        return 0;
    }

    static int RunUnmount(UnmountOptions o)
    {
        using var dism = new DismService();
        dism.UnmountImage(o.Commit);
        Console.WriteLine($"Unmounted {o.MountPath} (commit={o.Commit})");
        return 0;
    }

    static int RunComponents(ComponentsOptions o)
    {
        using var dism = new DismService();
        var mgr = new ComponentManager(dism);
        if (o.Features)
        {
            foreach (var f in mgr.ListFeatures(o.MountPath))
                Console.WriteLine($"{f.State,-20} {f.Name}");
        }
        else
        {
            foreach (var c in mgr.ListComponents(o.MountPath))
                Console.WriteLine($"{c.ReleaseType,-20} {c.State,-20} {c.Name}");
        }
        return 0;
    }

    static int RunRemovePackage(RemovePackageOptions o)
    {
        using var dism = new DismService();
        var mgr = new ComponentManager(dism);
        mgr.RemovePackage(o.MountPath, o.PackageName, new Progress<string>(Console.WriteLine));
        return 0;
    }

    static int RunAddDriver(AddDriverOptions o)
    {
        using var dism = new DismService();
        var mgr = new DriverManager(dism);
        if (Directory.Exists(o.DriverPath))
            mgr.AddDriversFromFolder(o.MountPath, o.DriverPath, forceUnsigned: o.Unsigned,
                progress: new Progress<string>(Console.WriteLine));
        else
            mgr.AddDriver(o.MountPath, o.DriverPath, o.Unsigned,
                new Progress<string>(Console.WriteLine));
        return 0;
    }

    static int RunAddUpdate(AddUpdateOptions o)
    {
        using var dism = new DismService();
        var integrator = new UpdateIntegrator(dism);
        var progress = new Progress<string>(Console.WriteLine);
        if (Directory.Exists(o.PackagePath))
            integrator.IntegrateUpdatesFromFolder(o.MountPath, o.PackagePath, progress);
        else
            integrator.IntegrateUpdate(o.MountPath, o.PackagePath, progress);
        return 0;
    }

    static int RunTweak(TweakOptions o)
    {
        if (o.List)
        {
            Console.WriteLine("Available categories:");
            foreach (var g in TweakCatalog.All)
                Console.WriteLine($"  {g.Category,-25} {g.DisplayName}");
            return 0;
        }

        var applicator = new TweakApplicator();
        var progress = new Progress<string>(Console.WriteLine);

        if (o.All)
        {
            applicator.ApplyAll(progress);
        }
        else if (!string.IsNullOrEmpty(o.Categories))
        {
            var cats = o.Categories.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var groups = TweakCatalog.All
                .Where(g => cats.Any(c => g.Category.ToString().Equals(c, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            applicator.Apply(groups, progress);
        }
        else
        {
            Console.Error.WriteLine("Specify --all or --category. Use --list to see categories.");
            return 1;
        }
        return 0;
    }

    static int RunUnattended(UnattendedOptions o)
    {
        var config = new UnattendedConfig
        {
            ComputerName = o.ComputerName,
            TimeZone     = o.TimeZone,
            UILanguage   = o.Language,
            InputLocale  = o.Language,
            SystemLocale = o.Language,
            UserLocale   = o.Language,
            SkipOobe     = o.SkipOobe,
            DiskConfig   = new DiskConfiguration
            {
                PartitionStyle = o.Gpt ? PartitionStyle.GPT : PartitionStyle.MBR
            }
        };

        var gen = new UnattendedGenerator();
        gen.Generate(config, o.Output);
        Console.WriteLine($"Generated: {o.Output}");
        return 0;
    }
}
