using System;
using System.Collections.Generic;

namespace winAlert.Domain.Models;

/// <summary>
/// Represents the severity level of an alert.
/// </summary>
public enum AlertSeverity
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3,
    Info = 4
}

/// <summary>
/// Extension methods for AlertSeverity enum.
/// </summary>
public static class AlertSeverityExtensions
{
    private static readonly Dictionary<AlertSeverity, string> ColorHexMap = new()
    {
        { AlertSeverity.Critical, "#FF5252" },
        { AlertSeverity.High, "#FF9800" },
        { AlertSeverity.Medium, "#FFC107" },
        { AlertSeverity.Low, "#66BB6A" },
        { AlertSeverity.Info, "#42A5F5" }
    };

    private static readonly Dictionary<AlertSeverity, string> DisplayNameMap = new()
    {
        { AlertSeverity.Critical, "Critical" },
        { AlertSeverity.High, "High" },
        { AlertSeverity.Medium, "Medium" },
        { AlertSeverity.Low, "Low" },
        { AlertSeverity.Info, "Info" }
    };

    private static readonly Dictionary<AlertSeverity, string> AudioFileMap = new()
    {
        { AlertSeverity.Critical, "alarm_critical.wav" },
        { AlertSeverity.High, "alert_high.wav" },
        { AlertSeverity.Medium, "alert_medium.wav" },
        { AlertSeverity.Low, "notification.wav" },
        { AlertSeverity.Info, "notification_info.wav" }
    };

    private static readonly Dictionary<AlertSeverity, float> DefaultVolumeMap = new()
    {
        { AlertSeverity.Critical, 1.0f },
        { AlertSeverity.High, 0.8f },
        { AlertSeverity.Medium, 0.6f },
        { AlertSeverity.Low, 0.4f },
        { AlertSeverity.Info, 0.3f }
    };

    /// <summary>
    /// Gets the hexadecimal color code for the severity level.
    /// </summary>
    public static string ToColorHex(this AlertSeverity severity) =>
        ColorHexMap.TryGetValue(severity, out var hex) ? hex : "#9999B8";

    /// <summary>
    /// Gets the display name for the severity level.
    /// </summary>
    public static string ToDisplayName(this AlertSeverity severity) =>
        DisplayNameMap.TryGetValue(severity, out var name) ? name : "Unknown";

    /// <summary>
    /// Gets the default audio file name for the severity level.
    /// </summary>
    public static string ToAudioFile(this AlertSeverity severity) =>
        AudioFileMap.TryGetValue(severity, out var file) ? file : "notification_info.wav";

    /// <summary>
    /// Gets the default volume level for the severity (0.0 to 1.0).
    /// </summary>
    public static float ToDefaultVolume(this AlertSeverity severity) =>
        DefaultVolumeMap.TryGetValue(severity, out var vol) ? vol : 0.5f;

    /// <summary>
    /// Indicates whether this severity level requires audio notification.
    /// </summary>
    public static bool RequiresAudio(this AlertSeverity severity) =>
        severity is AlertSeverity.Critical or AlertSeverity.High or AlertSeverity.Medium;

    /// <summary>
    /// Indicates whether this severity requires visual overlay.
    /// </summary>
    public static bool RequiresVisualOverlay(this AlertSeverity severity) =>
        severity == AlertSeverity.Critical;

    /// <summary>
    /// Gets the default auto-close seconds for this severity when not acknowledged.
    /// </summary>
    public static int GetDefaultAutoCloseSeconds(this AlertSeverity severity) =>
        severity switch
        {
            AlertSeverity.Critical => 0, // Never auto-close
            AlertSeverity.High => 0,     // Never auto-close
            AlertSeverity.Medium => 30,
            AlertSeverity.Low => 15,
            AlertSeverity.Info => 10,
            _ => 15
        };
}
