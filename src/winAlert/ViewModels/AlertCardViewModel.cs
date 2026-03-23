using System;
using winAlert.Domain.Models;

namespace winAlert.ViewModels;

/// <summary>
/// ViewModel for a single alert card.
/// </summary>
public sealed class AlertCardViewModel : ViewModelBase
{
    private readonly Alert _alert;
    private bool _isAcknowledged;
    private bool _isExpanded;

    public AlertCardViewModel(Alert alert)
    {
        _alert = alert;
        _isAcknowledged = alert.IsAcknowledged;
    }

    public Guid Id => _alert.Id;
    public AlertSeverity Severity => _alert.Severity;
    public string Source => _alert.Source;
    public string Title => _alert.Title;
    public string Message => _alert.Message;
    public DateTime Timestamp => _alert.Timestamp;
    public DateTime ReceivedAt => _alert.ReceivedAt;
    public string TruncatedMessage => _alert.GetTruncatedMessage(150);
    public string DisplayTime => _alert.GetDisplayTime();
    public bool RequireAcknowledgment => _alert.RequireAcknowledgment;

    public bool IsAcknowledged
    {
        get => _isAcknowledged;
        private set => SetProperty(ref _isAcknowledged, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string SeverityDisplayName => _alert.Severity.ToDisplayName();
    public string SeverityColorHex => _alert.Severity.ToColorHex();

    public void MarkAcknowledged()
    {
        IsAcknowledged = true;
    }

    public Alert GetAlert() => _alert;
}
