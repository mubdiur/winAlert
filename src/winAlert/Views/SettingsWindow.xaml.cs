using System.Windows;
using winAlert.ViewModels;

namespace winAlert.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        _viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose()
    {
        _viewModel.RequestClose -= OnRequestClose;
        Close();
    }
}
