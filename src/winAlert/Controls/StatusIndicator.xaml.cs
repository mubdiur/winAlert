using System.Windows;
using System.Windows.Controls;

namespace winAlert.Controls;

/// <summary>
/// Interaction logic for StatusIndicator.xaml
/// </summary>
public partial class StatusIndicator : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty IsListeningProperty =
        DependencyProperty.Register(
            nameof(IsListening),
            typeof(bool),
            typeof(StatusIndicator),
            new PropertyMetadata(false));

    public bool IsListening
    {
        get => (bool)GetValue(IsListeningProperty);
        set => SetValue(IsListeningProperty, value);
    }

    public StatusIndicator()
    {
        InitializeComponent();
    }
}
