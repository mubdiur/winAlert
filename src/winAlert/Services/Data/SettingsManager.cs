using System;
using winAlert.Domain.Models;

namespace winAlert.Services.Data;

/// <summary>
/// Interface for settings management.
/// </summary>
public interface ISettingsManager
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings CurrentSettings { get; }

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves current settings to disk.
    /// </summary>
    void Save();

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    void Reset();

    /// <summary>
    /// Updates a specific setting.
    /// </summary>
    void UpdateSetting<T>(string propertyPath, T value);
}

/// <summary>
/// Manages application settings persistence.
/// </summary>
public sealed class SettingsManager : ISettingsManager
{
    private AppSettings _settings;

    public SettingsManager()
    {
        _settings = AppSettings.Load();
    }

    /// <inheritdoc />
    public AppSettings CurrentSettings => _settings;

    /// <inheritdoc />
    public void Load()
    {
        _settings = AppSettings.Load();
    }

    /// <inheritdoc />
    public void Save()
    {
        _settings.Save();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _settings = new AppSettings();
        _settings.Save();
    }

    /// <inheritdoc />
    public void UpdateSetting<T>(string propertyPath, T value)
    {
        // Simple property path-based update (e.g., "Network.Port")
        var parts = propertyPath.Split('.');

        if (parts.Length == 2)
        {
            var section = parts[0];
            var property = parts[1];

            switch (section)
            {
                case "Network":
                    if (property == "Port" && value is int port)
                        _settings.Network.Port = port;
                    else if (property == "MaxConcurrentConnections" && value is int maxConn)
                        _settings.Network.MaxConcurrentConnections = maxConn;
                    break;

                case "Audio":
                    if (property == "MasterEnabled" && value is bool masterEnabled)
                        _settings.Audio.MasterEnabled = masterEnabled;
                    else if (property == "MasterVolume" && value is float volume)
                        _settings.Audio.MasterVolume = volume;
                    break;

                case "Notifications":
                    if (property == "ShowOverlayForCritical" && value is bool showOverlay)
                        _settings.Notifications.ShowOverlayForCritical = showOverlay;
                    else if (property == "FlashTaskbar" && value is bool flash)
                        _settings.Notifications.FlashTaskbar = flash;
                    break;

                case "Behavior":
                    if (property == "MinimizeToTray" && value is bool minimizeToTray)
                        _settings.Behavior.MinimizeToTray = minimizeToTray;
                    else if (property == "StartMinimized" && value is bool startMinimized)
                        _settings.Behavior.StartMinimized = startMinimized;
                    else if (property == "AlwaysOnTop" && value is bool alwaysOnTop)
                        _settings.Behavior.AlwaysOnTop = alwaysOnTop;
                    break;
            }

            Save();
        }
    }
}
