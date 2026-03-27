using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using WinImageTool.Core.Unattended;

namespace Cleanse11.ViewModels;

public class UnattendedViewModel : ViewModelBase
{
    private string _computerName = string.Empty;
    private string _timeZone = "UTC";
    private string _language = "en-US";
    private bool _skipOobe = true;
    private bool _acceptEula = true;
    private bool _useGpt = true;
    private bool _useAutoLogon;
    private string _autoLogonUser = string.Empty;
    private string _autoLogonPass = string.Empty;
    private string _status = string.Empty;
    private string _previewXml = string.Empty;

    public string ComputerName  { get => _computerName; set { Set(ref _computerName, value); Refresh(); } }
    public string TimeZone       { get => _timeZone;     set { Set(ref _timeZone, value);     Refresh(); } }
    public string Language       { get => _language;     set { Set(ref _language, value);     Refresh(); } }
    public bool SkipOobe         { get => _skipOobe;     set { Set(ref _skipOobe, value);     Refresh(); } }
    public bool AcceptEula       { get => _acceptEula;   set { Set(ref _acceptEula, value);   Refresh(); } }
    public bool UseGpt           { get => _useGpt;       set { Set(ref _useGpt, value);       Refresh(); } }
    public bool UseAutoLogon     { get => _useAutoLogon; set { Set(ref _useAutoLogon, value); Refresh(); } }
    public string AutoLogonUser  { get => _autoLogonUser; set { Set(ref _autoLogonUser, value); Refresh(); } }
    public string AutoLogonPass  { get => _autoLogonPass; set { Set(ref _autoLogonPass, value); Refresh(); } }
    public string Status         { get => _status;       set => Set(ref _status, value); }
    public string PreviewXml     { get => _previewXml;   set => Set(ref _previewXml, value); }

    public ObservableCollection<string> CommonTimeZones { get; } =
    [
        "UTC", "Eastern Standard Time", "Central Standard Time",
        "Mountain Standard Time", "Pacific Standard Time",
        "GMT Standard Time", "Central Europe Standard Time",
        "Romance Standard Time", "Tokyo Standard Time",
        "China Standard Time", "India Standard Time"
    ];

    public ObservableCollection<string> CommonLanguages { get; } =
    [
        "en-US", "en-GB", "de-DE", "fr-FR", "es-ES",
        "it-IT", "ja-JP", "ko-KR", "zh-CN", "zh-TW", "pt-BR"
    ];

    public RelayCommand SaveCommand   { get; }
    public RelayCommand CopyCommand   { get; }

    public UnattendedViewModel()
    {
        SaveCommand   = new RelayCommand(Save);
        CopyCommand   = new RelayCommand(Copy);
        Refresh();
    }

    private UnattendedConfig BuildConfig()
    {
        var cfg = new UnattendedConfig
        {
            ComputerName = ComputerName,
            TimeZone     = TimeZone,
            UILanguage   = Language,
            InputLocale  = Language,
            SystemLocale = Language,
            UserLocale   = Language,
            SkipOobe     = SkipOobe,
            AcceptEula   = AcceptEula,
            DiskConfig   = new DiskConfiguration
            {
                PartitionStyle = UseGpt ? PartitionStyle.GPT : PartitionStyle.MBR
            }
        };

        if (UseAutoLogon && !string.IsNullOrWhiteSpace(AutoLogonUser))
            cfg.AutoLogon = new AutoLogon { Username = AutoLogonUser, Password = AutoLogonPass, Count = 1 };

        return cfg;
    }

    private void Refresh()
    {
        try { PreviewXml = new UnattendedGenerator().GenerateToString(BuildConfig()); }
        catch { PreviewXml = string.Empty; }
    }

    private void Save()
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Save autounattend.xml",
            FileName   = "autounattend.xml",
            Filter     = "XML files (*.xml)|*.xml",
            DefaultExt = "xml"
        };
        if (dlg.ShowDialog() != true) return;
        new UnattendedGenerator().Generate(BuildConfig(), dlg.FileName);
        Status = $"Saved: {dlg.FileName}";
    }

    private void Copy()
    {
        if (!string.IsNullOrEmpty(PreviewXml))
        {
            Clipboard.SetText(PreviewXml);
            Status = "Copied to clipboard.";
        }
    }
}
