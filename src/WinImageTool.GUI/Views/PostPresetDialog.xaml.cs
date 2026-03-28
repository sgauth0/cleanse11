using System.Windows;

namespace Cleanse11.Views;

public partial class PostPresetDialog : Window
{
    public bool BuildIsoRequested { get; private set; }

    public PostPresetDialog(string presetName)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        MessageText.Text = $"The '{presetName}' preset has been applied to your image.\n\nWould you like to build a bootable ISO now, or continue customizing?";
    }

    private void BuildIsoClick(object sender, RoutedEventArgs e)
    {
        BuildIsoRequested = true;
        DialogResult = true;
        Close();
    }

    private void ContinueClick(object sender, RoutedEventArgs e)
    {
        BuildIsoRequested = false;
        DialogResult = false;
        Close();
    }
}