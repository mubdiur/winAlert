namespace winAlert.Domain.Events;

/// <summary>
/// Event raised when application settings are changed.
/// </summary>
public sealed class SettingsChangedEvent : IEvent
{
    /// <summary>
    /// Name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// The new value after change.
    /// </summary>
    public object? NewValue { get; }

    public SettingsChangedEvent(string propertyName, object? newValue)
    {
        PropertyName = propertyName;
        NewValue = newValue;
    }
}
