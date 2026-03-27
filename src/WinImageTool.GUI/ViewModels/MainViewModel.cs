using System.Diagnostics;
using System.Security.Principal;
using WinImageTool.Core.Imaging;
using WinImageTool.Core.Settings;

namespace Cleanse11.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DismService _dism;
    private ViewModelBase _currentPage;
    private string _navSelection = "Image";
    private bool _isAdmin;

    public ImageViewModel      ImageVM      { get; }
    public ComponentsViewModel ComponentsVM { get; }
    public DriversViewModel    DriversVM    { get; }
    public UpdatesViewModel    UpdatesVM    { get; }
    public TweaksViewModel     TweaksVM     { get; }
    public DebloatViewModel    DebloatVM    { get; }
    public UnattendedViewModel UnattendedVM { get; }
    public SettingsViewModel   SettingsVM   { get; }
    public AppSettings         Settings     { get; }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => Set(ref _currentPage, value);
    }

    public string NavSelection
    {
        get => _navSelection;
        set
        {
            Set(ref _navSelection, value);
            CurrentPage = value switch
            {
                "Image"      => ImageVM,
                "Debloat"    => DebloatVM,
                "Components" => ComponentsVM,
                "Drivers"    => DriversVM,
                "Updates"    => UpdatesVM,
                "Tweaks"     => TweaksVM,
                "Unattended" => UnattendedVM,
                "Settings"   => SettingsVM,
                _            => ImageVM
            };

            var mountPath = ImageVM.MountPath;
            ComponentsVM.SetMountPath(mountPath);
            DriversVM.SetMountPath(mountPath);
            UpdatesVM.SetMountPath(mountPath);
            DebloatVM.SetMountPath(mountPath);
        }
    }

    public RelayCommand<string> NavCommand { get; }

    public string WindowTitle => "Cleanse11";
    public string Version     => "1.0";
    
    public bool IsAdministrator => _isAdmin;
    
    public string AdminStatusText => _isAdmin 
        ? "✓ Running as Administrator" 
        : "⚠️ NOT Running as Administrator";

    public MainViewModel()
    {
        _isAdmin = CheckAdmin();
        Settings     = AppSettings.Load();
        _dism        = new DismService();
        ImageVM      = new ImageViewModel(_dism, Settings);
        ComponentsVM = new ComponentsViewModel(_dism);
        DriversVM    = new DriversViewModel(_dism);
        UpdatesVM    = new UpdatesViewModel(_dism);
        TweaksVM     = new TweaksViewModel();
        DebloatVM    = new DebloatViewModel();
        UnattendedVM = new UnattendedViewModel();
        SettingsVM   = new SettingsViewModel(_dism);
        _currentPage = ImageVM;

        ImageVM.MountStateChanged += () =>
        {
            var mountPath = ImageVM.MountPath;
            ComponentsVM.SetMountPath(mountPath);
            DriversVM.SetMountPath(mountPath);
            UpdatesVM.SetMountPath(mountPath);
            DebloatVM.SetMountPath(mountPath);
        };

        NavCommand = new RelayCommand<string>(page => NavSelection = page ?? "Image");
    }
    
    private static bool CheckAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
