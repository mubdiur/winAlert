using System.Windows.Input;
using winAlert.Domain.Models;
using winAlert.Services.Data;

namespace winAlert.ViewModels;

/// <summary>
/// ViewModel for the settings window.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsManager _settingsManager;
    private readonly AppSettings _settings;

    private int _port;
    private bool _audioEnabled;
    private float _masterVolume;
    private bool _showOverlayForCritical;
    private bool _flashTaskbar;
    private int _sirenDelaySeconds;
    private bool _minimizeToTray;
    private bool _startMinimized;
    private bool _alwaysOnTop;

    public SettingsViewModel(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _settings = settingsManager.CurrentSettings;

        // Load current values
        _port = _settings.Network.Port;
        _audioEnabled = _settings.Audio.MasterEnabled;
        _masterVolume = _settings.Audio.MasterVolume;
        _showOverlayForCritical = _settings.Notifications.ShowOverlayForCritical;
        _flashTaskbar = _settings.Notifications.FlashTaskbar;
        _sirenDelaySeconds = _settings.Notifications.SirenDelaySeconds;
        _minimizeToTray = _settings.Behavior.MinimizeToTray;
        _startMinimized = _settings.Behavior.StartMinimized;
        _alwaysOnTop = _settings.Behavior.AlwaysOnTop;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);
    }

    // Properties
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public bool AudioEnabled
    {
        get => _audioEnabled;
        set => SetProperty(ref _audioEnabled, value);
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set => SetProperty(ref _masterVolume, value);
    }

    public bool ShowOverlayForCritical
    {
        get => _showOverlayForCritical;
        set => SetProperty(ref _showOverlayForCritical, value);
    }

    public bool FlashTaskbar
    {
        get => _flashTaskbar;
        set => SetProperty(ref _flashTaskbar, value);
    }

    public int SirenDelaySeconds
    {
        get => _sirenDelaySeconds;
        set => SetProperty(ref _sirenDelaySeconds, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetProperty(ref _alwaysOnTop, value);
    }

    // Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    // Events
    public event System.Action? RequestClose;
    public event System.Action? SettingsSaved;

    // Command implementations
    private void Save()
    {
        _settings.Network.Port = Port;
        _settings.Audio.MasterEnabled = AudioEnabled;
        _settings.Audio.MasterVolume = MasterVolume;
        _settings.Notifications.ShowOverlayForCritical = ShowOverlayForCritical;
        _settings.Notifications.FlashTaskbar = FlashTaskbar;
        _settings.Notifications.SirenDelaySeconds = SirenDelaySeconds;
        _settings.Behavior.MinimizeToTray = MinimizeToTray;
        _settings.Behavior.StartMinimized = StartMinimized;
        _settings.Behavior.AlwaysOnTop = AlwaysOnTop;

        _settingsManager.Save();
        SettingsSaved?.Invoke();
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    private void Reset()
    {
        _settingsManager.Reset();
        var reset = _settingsManager.CurrentSettings;

        Port = reset.Network.Port;
        AudioEnabled = reset.Audio.MasterEnabled;
        MasterVolume = reset.Audio.MasterVolume;
        ShowOverlayForCritical = reset.Notifications.ShowOverlayForCritical;
        FlashTaskbar = reset.Notifications.FlashTaskbar;
        SirenDelaySeconds = reset.Notifications.SirenDelaySeconds;
        MinimizeToTray = reset.Behavior.MinimizeToTray;
        StartMinimized = reset.Behavior.StartMinimized;
        AlwaysOnTop = reset.Behavior.AlwaysOnTop;
    }
}
