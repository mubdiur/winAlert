namespace winAlert.Domain.Events;

/// <summary>
/// Event raised when an alert is acknowledged by the user.
/// </summary>
public sealed class AlertAcknowledgedEvent : IAlertEvent
{
    /// <inheritdoc />
    public Guid AlertId { get; }

    /// <summary>
    /// When the alert was acknowledged.
    /// </summary>
    public DateTime AcknowledgedAt { get; }

    /// <summary>
    /// Response time in milliseconds from receipt to acknowledgment.
    /// </summary>
    public long ResponseTimeMs { get; }

    public AlertAcknowledgedEvent(Guid alertId, DateTime acknowledgedAt, long responseTimeMs)
    {
        AlertId = alertId;
        AcknowledgedAt = acknowledgedAt;
        ResponseTimeMs = responseTimeMs;
    }
}
