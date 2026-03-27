namespace WinImageTool.Core.Unattended;

public class UnattendedConfig
{
    public string ComputerName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public string UILanguage { get; set; } = "en-US";
    public string InputLocale { get; set; } = "en-US";
    public string SystemLocale { get; set; } = "en-US";
    public string UserLocale { get; set; } = "en-US";
    public string ProductKey { get; set; } = string.Empty;
    public bool SkipOobe { get; set; } = true;
    public bool AcceptEula { get; set; } = true;
    public string RegisteredOwner { get; set; } = string.Empty;
    public string RegisteredOrganization { get; set; } = string.Empty;
    public List<LocalAccount> LocalAccounts { get; set; } = [];
    public AutoLogon? AutoLogon { get; set; }
    public DiskConfiguration? DiskConfig { get; set; }
}

public class LocalAccount
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdministrator { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class AutoLogon
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

public class DiskConfiguration
{
    public int DiskId { get; set; } = 0;
    public bool WipeClean { get; set; } = true;
    public PartitionStyle PartitionStyle { get; set; } = PartitionStyle.GPT;
}

public enum PartitionStyle { MBR, GPT }
