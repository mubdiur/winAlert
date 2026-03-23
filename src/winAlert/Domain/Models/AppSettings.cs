using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace winAlert.Domain.Models;

/// <summary>
/// Application settings that can be persisted to JSON.
/// </summary>
public class AppSettings
{
    public NetworkSettings Network { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "winAlert",
        "settings.json");

    /// <summary>
    /// Loads settings from the default JSON file.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Return default settings on any error
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves settings to the default JSON file.
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail - logging not available at this level
        }
    }
}

/// <summary>
/// Network configuration settings.
/// </summary>
public class NetworkSettings
{
    public int Port { get; set; } = 8888;
    public int MaxConcurrentConnections { get; set; } = 50;
    public int ReceiveBufferSize { get; set; } = 8192;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Audio notification settings.
/// </summary>
public class AudioSettings
{
    public bool MasterEnabled { get; set; } = true;
    public float MasterVolume { get; set; } = 0.8f;
    public Dictionary<string, bool> SeverityMutes { get; set; } = new()
    {
        { "critical", false },
        { "high", false },
        { "medium", false },
        { "low", true },
        { "info", true }
    };
}

/// <summary>
/// Visual notification settings.
/// </summary>
public class NotificationSettings
{
    public bool CriticalRequireAck { get; set; } = true;
    public bool HighRequireAck { get; set; } = true;
    public int HighAckTimeoutSeconds { get; set; } = 60;
    public int MediumAutoCloseSeconds { get; set; } = 30;
    public int LowAutoCloseSeconds { get; set; } = 15;
    public int InfoAutoCloseSeconds { get; set; } = 10;
    public int EscalationThresholdSeconds { get; set; } = 90;
    public bool ShowOverlayForCritical { get; set; } = true;
    public bool ShowOverlayForHigh { get; set; } = false;
    public bool FlashTaskbar { get; set; } = true;
}

/// <summary>
/// Application behavior settings.
/// </summary>
public class BehaviorSettings
{
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool AlwaysOnTop { get; set; } = false;
}

/// <summary>
/// Logging configuration settings.
/// </summary>
public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public int MaxLogFileSizeMB { get; set; } = 10;
    public int MaxLogFiles { get; set; } = 5;
}
