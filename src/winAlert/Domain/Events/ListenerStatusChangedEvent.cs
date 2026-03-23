namespace winAlert.Domain.Events;

/// <summary>
/// Event raised when the listener status changes.
/// </summary>
public sealed class ListenerStatusChangedEvent : IEvent
{
    /// <summary>
    /// Whether the listener is currently active.
    /// </summary>
    public bool IsListening { get; }

    /// <summary>
    /// Current port being listened on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Number of active connections.
    /// </summary>
    public int ConnectionCount { get; }

    /// <summary>
    /// Error message if listening failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; }

    public ListenerStatusChangedEvent(bool isListening, int port, int connectionCount, string? errorMessage = null)
    {
        IsListening = isListening;
        Port = port;
        ConnectionCount = connectionCount;
        ErrorMessage = errorMessage;
    }
}
