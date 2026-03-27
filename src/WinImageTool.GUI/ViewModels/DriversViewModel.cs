using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WinImageTool.Core.Drivers;
using WinImageTool.Core.Imaging;

namespace Cleanse11.ViewModels;

public class DriversViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private bool _isBusy;
    private string _status = string.Empty;
    private bool _forceUnsigned;
    private string _mountPath = string.Empty;

    public ObservableCollection<DriverInfo> Drivers { get; } = [];
    public bool IsBusy   { get => _isBusy;  set => Set(ref _isBusy, value); }
    public string Status { get => _status;  set => Set(ref _status, value); }
    public bool ForceUnsigned { get => _forceUnsigned; set => Set(ref _forceUnsigned, value); }
    public bool ShowMountHint => string.IsNullOrEmpty(_mountPath);

    public RelayCommand LoadCommand          { get; }
    public RelayCommand AddDriverFileCommand { get; }
    public RelayCommand AddDriverFolderCommand { get; }

    public DriversViewModel(DismService dism)
    {
        _dism = dism;
        LoadCommand            = new RelayCommand(Load,            () => !IsBusy);
        AddDriverFileCommand   = new RelayCommand(AddDriverFile,   () => !IsBusy);
        AddDriverFolderCommand = new RelayCommand(AddDriverFolder, () => !IsBusy);
    }

    public void SetMountPath(string path) 
    { 
        _mountPath = path; 
        Notify(nameof(ShowMountHint)); 
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_mountPath)) { Status = "No image mounted. Mount an image first."; return; }
        if (!Directory.Exists(_mountPath)) { Status = $"Mount path does not exist: {_mountPath}"; return; }
        IsBusy = true;
        Status = $"Loading drivers from: {_mountPath}";
        Task.Run(() =>
        {
            try
            {
                var drivers = new DriverManager(_dism).ListDrivers(_mountPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Drivers.Clear();
                    foreach (var d in drivers) Drivers.Add(d);
                    Status = $"{drivers.Count} driver(s) found.";
                });
            }
            catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => Status = $"Error: {ex.Message}"); }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }

    private void AddDriverFile()
    {
        var dlg = new OpenFileDialog { Title = "Select Driver INF", Filter = "INF files (*.inf)|*.inf" };
        if (dlg.ShowDialog() != true) return;
        RunAdd(dlg.FileName, false);
    }

    private void AddDriverFolder()
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog { Description = "Select folder containing driver INF files", UseDescriptionForTitle = true };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        RunAdd(dlg.SelectedPath, true);
    }

    private void RunAdd(string path, bool isFolder)
    {
        IsBusy = true;
        var progress = new Progress<string>(msg => Application.Current.Dispatcher.Invoke(() => Status = msg));
        var mgr = new DriverManager(_dism);
        Task.Run(() =>
        {
            try
            {
                if (isFolder) mgr.AddDriversFromFolder(_mountPath, path, forceUnsigned: ForceUnsigned, progress: progress);
                else          mgr.AddDriver(_mountPath, path, ForceUnsigned, progress);
                Application.Current.Dispatcher.Invoke(Load);
            }
            catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => Status = $"Error: {ex.Message}"); }
            finally { Application.Current.Dispatcher.Invoke(() => IsBusy = false); }
        });
    }
}
