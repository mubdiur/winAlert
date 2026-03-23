using System.Windows;
using winAlert.ViewModels;

namespace winAlert.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnStateChanged(System.EventArgs e)
    {
        base.OnStateChanged(e);

        // Minimize to tray functionality handled by ViewModel
    }
}
