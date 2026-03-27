using System.Windows;

namespace Cleanse11.Views;

public partial class SelectMountDialog : Window
{
    public string? SelectedPath { get; private set; }

    public SelectMountDialog(List<MountedImageItem> mounts)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        MountList.ItemsSource = mounts;
        if (mounts.Count > 0)
            MountList.SelectedIndex = 0;
    }

    private void ConnectClick(object sender, RoutedEventArgs e)
    {
        if (MountList.SelectedItem is MountedImageItem selected)
        {
            SelectedPath = selected.Path;
            DialogResult = true;
        }
        Close();
    }
}
