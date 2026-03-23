using winAlert.Domain.Models;

namespace winAlert.Domain.Events;

/// <summary>
/// Event raised when a new alert is received from the network.
/// </summary>
public sealed class AlertReceivedEvent : IAlertEvent
{
    /// <summary>
    /// The alert that was received.
    /// </summary>
    public Alert Alert { get; }

    /// <summary>
    /// The notification plan for this alert.
    /// </summary>
    public NotificationPlan NotificationPlan { get; }

    /// <inheritdoc />
    public Guid AlertId => Alert.Id;

    public AlertReceivedEvent(Alert alert, NotificationPlan notificationPlan)
    {
        Alert = alert;
        NotificationPlan = notificationPlan;
    }
}
