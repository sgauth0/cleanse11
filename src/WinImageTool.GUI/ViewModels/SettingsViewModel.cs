using System.IO;
using System.Windows.Input;
using WinImageTool.Core.Imaging;

namespace Cleanse11.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private string _scratchPath = string.Empty;
    private string _workingDir = string.Empty;

    public string ScratchPath
    {
        get => _scratchPath;
        set
        {
            if (Equals(_scratchPath, value)) return;
            _scratchPath = value;
            _dism.SetScratchDir(string.IsNullOrWhiteSpace(value) ? null : value);
            Notify();
        }
    }

    public string WorkingDir
    {
        get => _workingDir;
        set { if (!Equals(_workingDir, value)) { _workingDir = value; Notify(nameof(WorkingDir)); } }
    }

    public RelayCommand BrowseScratchCommand { get; }
    public RelayCommand ClearScratchCommand { get; }
    public RelayCommand BrowseWorkingDirCommand { get; }

    public SettingsViewModel(DismService dism)
    {
        _dism = dism;
        _scratchPath = _dism.ScratchDir ?? string.Empty;
        _workingDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinImageTool", "WorkingDir");
        BrowseScratchCommand = new RelayCommand(BrowseScratch);
        ClearScratchCommand = new RelayCommand(ClearScratch);
        BrowseWorkingDirCommand = new RelayCommand(BrowseWorkingDir);
    }

    private void BrowseScratch()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select default scratch directory for DISM operations",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            ScratchPath = dlg.SelectedPath;
    }

    private void ClearScratch()
    {
        ScratchPath = string.Empty;
    }

    private void BrowseWorkingDir()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select working directory for WIM cache and temp files",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            WorkingDir = dlg.SelectedPath;
    }
}
