using winAlert.Domain.Models;

namespace winAlert.Domain.Events;

/// <summary>
/// Event raised when an alert is escalated due to lack of acknowledgment.
/// </summary>
public sealed class AlertEscalatedEvent : IAlertEvent
{
    /// <inheritdoc />
    public Guid AlertId { get; }

    /// <summary>
    /// Seconds since the alert was received.
    /// </summary>
    public int SecondsSinceReceived { get; }

    /// <summary>
    /// The updated notification plan with escalation applied.
    /// </summary>
    public NotificationPlan NewNotificationPlan { get; }

    public AlertEscalatedEvent(Guid alertId, int secondsSinceReceived, NotificationPlan newNotificationPlan)
    {
        AlertId = alertId;
        SecondsSinceReceived = secondsSinceReceived;
        NewNotificationPlan = newNotificationPlan;
    }
}
