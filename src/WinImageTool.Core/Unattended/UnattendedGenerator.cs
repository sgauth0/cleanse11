using System.Text;
using System.Xml.Linq;

namespace WinImageTool.Core.Unattended;

public class UnattendedGenerator
{
    private static readonly XNamespace Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";
    private static readonly XNamespace Unattend = "urn:schemas-microsoft-com:unattend";

    public void Generate(UnattendedConfig config, string outputPath)
    {
        var doc = BuildDocument(config);
        doc.Save(outputPath, SaveOptions.None);
    }

    public string GenerateToString(UnattendedConfig config)
    {
        var doc = BuildDocument(config);
        var sb = new StringBuilder();
        using var writer = new System.IO.StringWriter(sb);
        doc.Save(writer, SaveOptions.None);
        return sb.ToString();
    }

    private XDocument BuildDocument(UnattendedConfig cfg)
    {
        var root = new XElement(Unattend + "unattend",
            new XAttribute(XNamespace.Xmlns + "wcm", Wcm),
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));

        root.Add(BuildWindowsPEPass(cfg));
        root.Add(BuildSpecializePass(cfg));
        root.Add(BuildOobePass(cfg));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
    }

    private XElement BuildWindowsPEPass(UnattendedConfig cfg)
    {
        var pass = new XElement(Unattend + "settings", new XAttribute("pass", "windowsPE"));

        // International settings
        pass.Add(new XElement(Unattend + "component",
            new XAttribute("name", "Microsoft-Windows-International-Core-WinPE"),
            new XAttribute("processorArchitecture", "amd64"),
            new XAttribute("publicKeyToken", "31bf3856ad364e35"),
            new XAttribute("language", "neutral"),
            new XAttribute("versionScope", "nonSxS"),
            new XAttribute(Wcm + "action", "add"),
            new XElement(Unattend + "SetupUILanguage",
                new XElement(Unattend + "UILanguage", cfg.UILanguage)),
            new XElement(Unattend + "InputLocale", cfg.InputLocale),
            new XElement(Unattend + "SystemLocale", cfg.SystemLocale),
            new XElement(Unattend + "UILanguage", cfg.UILanguage),
            new XElement(Unattend + "UserLocale", cfg.UserLocale)));

        // Disk config
        if (cfg.DiskConfig != null)
        {
            pass.Add(BuildDiskConfig(cfg.DiskConfig));
        }

        return pass;
    }

    private XElement BuildSpecializePass(UnattendedConfig cfg)
    {
        var pass = new XElement(Unattend + "settings", new XAttribute("pass", "specialize"));

        var comp = new XElement(Unattend + "component",
            new XAttribute("name", "Microsoft-Windows-Shell-Setup"),
            new XAttribute("processorArchitecture", "amd64"),
            new XAttribute("publicKeyToken", "31bf3856ad364e35"),
            new XAttribute("language", "neutral"),
            new XAttribute("versionScope", "nonSxS"));

        if (!string.IsNullOrEmpty(cfg.ComputerName))
            comp.Add(new XElement(Unattend + "ComputerName", cfg.ComputerName));

        comp.Add(new XElement(Unattend + "TimeZone", cfg.TimeZone));

        if (!string.IsNullOrEmpty(cfg.RegisteredOwner))
            comp.Add(new XElement(Unattend + "RegisteredOwner", cfg.RegisteredOwner));

        if (!string.IsNullOrEmpty(cfg.RegisteredOrganization))
            comp.Add(new XElement(Unattend + "RegisteredOrganization", cfg.RegisteredOrganization));

        if (!string.IsNullOrEmpty(cfg.ProductKey))
            comp.Add(new XElement(Unattend + "ProductKey", cfg.ProductKey));

        pass.Add(comp);
        return pass;
    }

    private XElement BuildOobePass(UnattendedConfig cfg)
    {
        var pass = new XElement(Unattend + "settings", new XAttribute("pass", "oobeSystem"));

        var comp = new XElement(Unattend + "component",
            new XAttribute("name", "Microsoft-Windows-Shell-Setup"),
            new XAttribute("processorArchitecture", "amd64"),
            new XAttribute("publicKeyToken", "31bf3856ad364e35"),
            new XAttribute("language", "neutral"),
            new XAttribute("versionScope", "nonSxS"));

        if (cfg.SkipOobe)
        {
            comp.Add(new XElement(Unattend + "OOBE",
                new XElement(Unattend + "HideEULAPage", cfg.AcceptEula ? "true" : "false"),
                new XElement(Unattend + "HideLocalAccountScreen", "false"),
                new XElement(Unattend + "HideOEMRegistrationScreen", "true"),
                new XElement(Unattend + "HideOnlineAccountScreens", "true"),
                new XElement(Unattend + "HideWirelessSetupInOOBE", "true"),
                new XElement(Unattend + "NetworkLocation", "Home"),
                new XElement(Unattend + "SkipUserOOBE", "true"),
                new XElement(Unattend + "SkipMachineOOBE", "true")));
        }

        if (cfg.LocalAccounts.Count > 0)
        {
            var accounts = new XElement(Unattend + "UserAccounts",
                new XElement(Unattend + "LocalAccounts"));

            foreach (var acct in cfg.LocalAccounts)
            {
                accounts.Element(Unattend + "LocalAccounts")!.Add(
                    new XElement(Unattend + "LocalAccount",
                        new XAttribute(Wcm + "action", "add"),
                        new XElement(Unattend + "Password",
                            new XElement(Unattend + "Value", acct.PasswordHash),
                            new XElement(Unattend + "PlainText", "false")),
                        new XElement(Unattend + "Description", acct.DisplayName),
                        new XElement(Unattend + "DisplayName", acct.DisplayName),
                        new XElement(Unattend + "Group", acct.IsAdministrator ? "Administrators" : "Users"),
                        new XElement(Unattend + "Name", acct.Username)));
            }
            comp.Add(accounts);
        }

        if (cfg.AutoLogon != null)
        {
            comp.Add(new XElement(Unattend + "AutoLogon",
                new XElement(Unattend + "Password",
                    new XElement(Unattend + "Value", cfg.AutoLogon.Password),
                    new XElement(Unattend + "PlainText", "true")),
                new XElement(Unattend + "Enabled", "true"),
                new XElement(Unattend + "LogonCount", cfg.AutoLogon.Count),
                new XElement(Unattend + "Username", cfg.AutoLogon.Username)));
        }

        pass.Add(comp);
        return pass;
    }

    private XElement BuildDiskConfig(DiskConfiguration diskCfg)
    {
        bool isGpt = diskCfg.PartitionStyle == PartitionStyle.GPT;

        var diskElem = new XElement(Unattend + "Disk",
            new XAttribute(Wcm + "action", "add"),
            new XElement(Unattend + "DiskID", diskCfg.DiskId),
            new XElement(Unattend + "WillWipeDisk", diskCfg.WipeClean ? "true" : "false"),
            new XElement(Unattend + "CreatePartitions",
                isGpt ? BuildGptPartitions() : BuildMbrPartitions()),
            new XElement(Unattend + "ModifyPartitions",
                isGpt ? BuildGptModifyPartitions() : BuildMbrModifyPartitions()));

        return new XElement(Unattend + "component",
            new XAttribute("name", "Microsoft-Windows-Setup"),
            new XAttribute("processorArchitecture", "amd64"),
            new XAttribute("publicKeyToken", "31bf3856ad364e35"),
            new XAttribute("language", "neutral"),
            new XAttribute("versionScope", "nonSxS"),
            new XElement(Unattend + "DiskConfiguration",
                new XElement(Unattend + "WillShowUI", "OnError"),
                diskElem));
    }

    private static IEnumerable<XElement> BuildGptPartitions()
    {
        yield return Partition("1", "EFI", "Primary", size: "100");
        yield return Partition("2", "MSR", "Primary", size: "16");
        yield return Partition("3", "Primary", "Primary", extend: true);
        yield return Partition("4", "Recovery", "Primary", size: "990");
    }

    private static IEnumerable<XElement> BuildGptModifyPartitions()
    {
        yield return ModifyPartition("1", "1", "FAT32", "System", active: false);
        yield return ModifyPartition("2", "2", null, null, active: false, noLetter: true);
        yield return ModifyPartition("3", "3", "NTFS", "Windows", active: false);
        yield return ModifyPartition("4", "4", "NTFS", "Recovery", active: false);
    }

    private static IEnumerable<XElement> BuildMbrPartitions()
    {
        yield return Partition("1", "Primary", "Primary", size: "350", active: true);
        yield return Partition("2", "Primary", "Primary", extend: true);
    }

    private static IEnumerable<XElement> BuildMbrModifyPartitions()
    {
        yield return ModifyPartition("1", "1", "NTFS", "System", active: true);
        yield return ModifyPartition("2", "2", "NTFS", "Windows", active: false);
    }

    private static XElement Partition(string order, string type, string partType,
        string? size = null, bool extend = false, bool active = false)
    {
        var p = new XElement(Unattend + "CreatePartition",
            new XAttribute(Wcm + "action", "add"),
            new XElement(Unattend + "Order", order),
            new XElement(Unattend + "Type", type));
        if (extend) p.Add(new XElement(Unattend + "Extend", "true"));
        else if (size != null) p.Add(new XElement(Unattend + "Size", size));
        if (active) p.Add(new XElement(Unattend + "Active", "true"));
        return p;
    }

    private static XElement ModifyPartition(string order, string partition,
        string? format, string? label, bool active = false, bool noLetter = false)
    {
        var p = new XElement(Unattend + "ModifyPartition",
            new XAttribute(Wcm + "action", "add"),
            new XElement(Unattend + "Order", order),
            new XElement(Unattend + "PartitionID", partition));
        if (format != null) p.Add(new XElement(Unattend + "Format", format));
        if (label != null) p.Add(new XElement(Unattend + "Label", label));
        if (active) p.Add(new XElement(Unattend + "Active", "true"));
        if (!noLetter && format != null)
            p.Add(new XElement(Unattend + "Letter", partition == "3" ? "C" : string.Empty));
        return p;
    }
}
