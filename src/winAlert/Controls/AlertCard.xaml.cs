using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using winAlert.ViewModels;

namespace winAlert.Controls;

/// <summary>
/// Interaction logic for AlertCard.xaml
/// </summary>
public partial class AlertCard : System.Windows.Controls.UserControl
{
    public AlertCard()
    {
        InitializeComponent();

        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AlertCardViewModel vm && !vm.IsAcknowledged)
        {
            vm.IsExpanded = !vm.IsExpanded;
        }
    }
}
