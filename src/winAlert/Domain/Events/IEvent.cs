namespace winAlert.Domain.Events;

/// <summary>
/// Base interface for all domain events.
/// </summary>
public interface IEvent
{
}

/// <summary>
/// Marker interface for alert-related events.
/// </summary>
public interface IAlertEvent : IEvent
{
    Guid AlertId { get; }
}
