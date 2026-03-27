using System.Windows.Controls;
using System.Windows.Input;
using Cleanse11.ViewModels;

namespace Cleanse11.Views;

public partial class TweaksView : System.Windows.Controls.UserControl
{
    public TweaksView()
    {
        InitializeComponent();
    }

    private void ToggleExpand(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border.DataContext is TweakGroupItem item)
        {
            item.IsExpanded = !item.IsExpanded;
            e.Handled = true;
        }
    }
}
