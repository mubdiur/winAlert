using System;
using System.Windows;
using winAlert.Domain.Models;
using winAlert.ViewModels;

namespace winAlert.Views;

/// <summary>
/// Interaction logic for AlertDetailWindow.xaml
/// </summary>
public partial class AlertDetailWindow : Window
{
    private readonly AlertCardViewModel _viewModel;

    public AlertDetailWindow(AlertCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = new AlertDetailViewModel(viewModel);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        // The acknowledgment is handled by the main ViewModel
        Close();
    }

    private sealed class AlertDetailViewModel
    {
        public AlertCardViewModel Alert { get; }
        public bool HasMetadata => Alert.GetAlert().Metadata.Count > 0;
        public bool CanAcknowledge => !Alert.IsAcknowledged && Alert.RequireAcknowledgment;

        public AlertDetailViewModel(AlertCardViewModel alert)
        {
            Alert = alert;
        }
    }
}
