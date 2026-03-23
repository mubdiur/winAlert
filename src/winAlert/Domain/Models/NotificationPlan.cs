namespace winAlert.Domain.Models;

/// <summary>
/// Defines how an alert notification should be delivered.
/// </summary>
public class NotificationPlan
{
    /// <summary>
    /// Whether to play audio for this notification.
    /// </summary>
    public bool PlayAudio { get; init; }

    /// <summary>
    /// The audio file to play.
    /// </summary>
    public string AudioFile { get; init; } = string.Empty;

    /// <summary>
    /// The volume level for audio playback (0.0 to 1.0).
    /// </summary>
    public float AudioVolume { get; init; } = 0.5f;

    /// <summary>
    /// Whether to show a visual overlay for this notification.
    /// </summary>
    public bool ShowOverlay { get; init; }

    /// <summary>
    /// Whether this notification requires user acknowledgment.
    /// </summary>
    public bool RequireAcknowledgment { get; init; }

    /// <summary>
    /// Seconds after which to auto-dismiss (0 = never).
    /// </summary>
    public int AutoCloseSeconds { get; init; }

    /// <summary>
    /// Whether escalation is enabled for unacknowledged alerts.
    /// </summary>
    public bool EscalationEnabled { get; init; }

    /// <summary>
    /// Whether to flash the taskbar.
    /// </summary>
    public bool TaskbarFlash { get; init; }

    /// <summary>
    /// Creates a default notification plan for the given severity.
    /// </summary>
    public static NotificationPlan ForSeverity(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => new NotificationPlan
            {
                PlayAudio = true,
                AudioFile = severity.ToAudioFile(),
                AudioVolume = severity.ToDefaultVolume(),
                ShowOverlay = true,
                RequireAcknowledgment = true,
                AutoCloseSeconds = 0,
                EscalationEnabled = true,
                TaskbarFlash = true
            },
            AlertSeverity.High => new NotificationPlan
            {
                PlayAudio = true,
                AudioFile = severity.ToAudioFile(),
                AudioVolume = severity.ToDefaultVolume(),
                ShowOverlay = false,
                RequireAcknowledgment = true,
                AutoCloseSeconds = 60,
                EscalationEnabled = true,
                TaskbarFlash = true
            },
            AlertSeverity.Medium => new NotificationPlan
            {
                PlayAudio = true,
                AudioFile = severity.ToAudioFile(),
                AudioVolume = severity.ToDefaultVolume(),
                ShowOverlay = false,
                RequireAcknowledgment = false,
                AutoCloseSeconds = 30,
                EscalationEnabled = false,
                TaskbarFlash = false
            },
            AlertSeverity.Low => new NotificationPlan
            {
                PlayAudio = false,
                AudioFile = severity.ToAudioFile(),
                AudioVolume = severity.ToDefaultVolume(),
                ShowOverlay = false,
                RequireAcknowledgment = false,
                AutoCloseSeconds = 15,
                EscalationEnabled = false,
                TaskbarFlash = false
            },
            AlertSeverity.Info => new NotificationPlan
            {
                PlayAudio = false,
                AudioFile = severity.ToAudioFile(),
                AudioVolume = severity.ToDefaultVolume(),
                ShowOverlay = false,
                RequireAcknowledgment = false,
                AutoCloseSeconds = 10,
                EscalationEnabled = false,
                TaskbarFlash = false
            },
            _ => new NotificationPlan()
        };
    }
}
