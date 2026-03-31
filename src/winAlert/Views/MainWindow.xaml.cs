using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using winAlert.ViewModels;

namespace winAlert.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        _viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsTopmost))
        {
            Topmost = _viewModel.IsTopmost;
        }
    }

    private void OnOpenSettingsRequested()
    {
        var settingsVm = App.ServiceProvider?.GetService(typeof(SettingsViewModel)) as SettingsViewModel;
        if (settingsVm != null)
        {
            var settingsWindow = new SettingsWindow(settingsVm)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }
    }

    protected override void OnStateChanged(System.EventArgs e)
    {
        base.OnStateChanged(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.OpenSettingsRequested -= OnOpenSettingsRequested;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosing(e);
    }
}
