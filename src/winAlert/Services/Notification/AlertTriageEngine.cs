using winAlert.Domain.Models;

namespace winAlert.Services.Notification;

/// <summary>
/// Determines the notification strategy for an alert based on severity and settings.
/// </summary>
public interface IAlertTriageEngine
{
    /// <summary>
    /// Creates a notification plan for the given alert and settings.
    /// </summary>
    NotificationPlan CreatePlan(Alert alert, AppSettings settings);
}

/// <summary>
/// Triage engine that determines how alerts should be notified.
/// </summary>
public sealed class AlertTriageEngine : IAlertTriageEngine
{
    /// <inheritdoc />
    public NotificationPlan CreatePlan(Alert alert, AppSettings settings)
    {
        var severity = alert.Severity;
        var notifications = settings.Notifications;
        var audio = settings.Audio;

        // Check if severity is muted
        var severityKey = severity.ToString().ToLowerInvariant();
        var isMuted = audio.SeverityMutes.TryGetValue(severityKey, out var muted) && muted;

        var plan = NotificationPlan.ForSeverity(severity);

        // Start with base plan and apply modifications
        var playAudio = !isMuted && audio.MasterEnabled && plan.PlayAudio;
        var audioVolume = audio.MasterEnabled ? audio.MasterVolume * plan.AudioVolume : 0f;
        var requireAck = plan.RequireAcknowledgment;
        var showOverlay = plan.ShowOverlay;
        var autoClose = plan.AutoCloseSeconds;
        var taskbarFlash = notifications.FlashTaskbar;

        // Override based on severity
        switch (severity)
        {
            case AlertSeverity.Critical when notifications.CriticalRequireAck:
                requireAck = true;
                showOverlay = notifications.ShowOverlayForCritical;
                break;
            case AlertSeverity.High when notifications.HighRequireAck:
                requireAck = true;
                autoClose = notifications.HighAckTimeoutSeconds;
                break;
            case AlertSeverity.Medium:
                autoClose = notifications.MediumAutoCloseSeconds;
                break;
            case AlertSeverity.Low:
                autoClose = notifications.LowAutoCloseSeconds;
                break;
            case AlertSeverity.Info:
                autoClose = notifications.InfoAutoCloseSeconds;
                break;
        }

        // Apply alert-specific overrides
        if (alert.RequireAcknowledgment)
        {
            requireAck = true;
        }

        if (alert.AutoCloseSeconds > 0)
        {
            autoClose = alert.AutoCloseSeconds;
        }

        return new NotificationPlan
        {
            PlayAudio = playAudio,
            AudioFile = plan.AudioFile,
            AudioVolume = audioVolume,
            ShowOverlay = showOverlay,
            RequireAcknowledgment = requireAck,
            AutoCloseSeconds = autoClose,
            EscalationEnabled = plan.EscalationEnabled,
            TaskbarFlash = taskbarFlash
        };
    }
}
