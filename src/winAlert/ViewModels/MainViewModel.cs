using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Serilog;
using winAlert.Domain.Events;
using winAlert.Domain.Models;
using winAlert.Services.Core;
using winAlert.Services.Data;
using winAlert.Services.Network;
using winAlert.Services.Notification;

namespace winAlert.ViewModels;

/// <summary>
/// Main window ViewModel.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IAlertListenerService _listenerService;
    private readonly IAlertRepository _alertRepository;
    private readonly IAudioNotificationService _audioService;
    private readonly IVisualNotificationService _visualService;
    private readonly ISettingsManager _settingsManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAlertTriageEngine _triageEngine;
    private readonly ILogger _logger;

    // Tracks escalation timers for unacknowledged alerts
    private readonly ConcurrentDictionary<Guid, DispatcherTimer> _escalationTimers = new();

    private bool _isListening;
    private int _port;
    private int _connectionCount;
    private bool _isAudioEnabled;
    private bool _hasActiveAlerts;
    private bool _showOverlay;
    private string _statusText = "Ready";
    private string _filterText = string.Empty;
    private AlertCardViewModel? _selectedAlert;

    public MainViewModel(
        IAlertListenerService listenerService,
        IAlertRepository alertRepository,
        IAudioNotificationService audioService,
        IVisualNotificationService visualService,
        ISettingsManager settingsManager,
        IEventAggregator eventAggregator,
        IAlertTriageEngine triageEngine,
        ILogger logger)
    {
        _listenerService = listenerService;
        _alertRepository = alertRepository;
        _audioService = audioService;
        _visualService = visualService;
        _settingsManager = settingsManager;
        _eventAggregator = eventAggregator;
        _triageEngine = triageEngine;
        _logger = logger;

        ActiveAlerts = new ObservableCollection<AlertCardViewModel>();
        AlertHistory = new ObservableCollection<AlertCardViewModel>();

        // Initialize commands
        StartListeningCommand = new RelayCommand(StartListening, () => !IsListening);
        StopListeningCommand = new RelayCommand(StopListening, () => IsListening);
        AcknowledgeAlertCommand = new RelayCommand(AcknowledgeAlert);
        AcknowledgeAllCommand = new RelayCommand(AcknowledgeAll, () => ActiveAlerts.Any());
        ClearLogCommand = new RelayCommand(ClearLog, () => AlertHistory.Any());
        ToggleAudioCommand = new RelayCommand(ToggleAudio);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
        ExitCommand = new RelayCommand(Exit);

        // Subscribe to events
        _eventAggregator.Subscribe<AlertReceivedEvent>(OnAlertReceived);
        _eventAggregator.Subscribe<AlertAcknowledgedEvent>(OnAlertAcknowledged);
        _eventAggregator.Subscribe<ListenerStatusChangedEvent>(OnListenerStatusChanged);

        // Initialize state
        _port = _settingsManager.CurrentSettings.Network.Port;
        _isAudioEnabled = _settingsManager.CurrentSettings.Audio.MasterEnabled;
    }

    // Properties
    public ObservableCollection<AlertCardViewModel> ActiveAlerts { get; }
    public ObservableCollection<AlertCardViewModel> AlertHistory { get; }

    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (SetProperty(ref _isListening, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public int ConnectionCount
    {
        get => _connectionCount;
        private set => SetProperty(ref _connectionCount, value);
    }

    public bool IsAudioEnabled
    {
        get => _isAudioEnabled;
        set
        {
            if (SetProperty(ref _isAudioEnabled, value))
            {
                _audioService.SetMute(!value);
                _settingsManager.UpdateSetting("Audio.MasterEnabled", value);
            }
        }
    }

    public bool HasActiveAlerts
    {
        get => _hasActiveAlerts;
        private set => SetProperty(ref _hasActiveAlerts, value);
    }

    public bool ShowOverlay
    {
        get => _showOverlay;
        set => SetProperty(ref _showOverlay, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                ApplyFilter();
            }
        }
    }

    public AlertCardViewModel? SelectedAlert
    {
        get => _selectedAlert;
        set => SetProperty(ref _selectedAlert, value);
    }

    public int CriticalCount => ActiveAlerts.Count(a => a.Severity == AlertSeverity.Critical);
    public int HighCount => ActiveAlerts.Count(a => a.Severity == AlertSeverity.High);
    public int MediumCount => ActiveAlerts.Count(a => a.Severity == AlertSeverity.Medium);

    // Commands
    public ICommand StartListeningCommand { get; }
    public ICommand StopListeningCommand { get; }
    public ICommand AcknowledgeAlertCommand { get; }
    public ICommand AcknowledgeAllCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand ToggleAudioCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand MinimizeToTrayCommand { get; }
    public ICommand ExitCommand { get; }

    // Event handlers
    private void OnAlertReceived(AlertReceivedEvent e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                var alert = e.Alert;
                var plan = e.NotificationPlan;

                _logger?.Information("[MAINVM] Processing alert: {AlertId} - {Title}", alert.Id, alert.Title);
                Debug.WriteLine($"[MAINVM] Processing alert: {alert.Id} - {alert.Title}");

                // Add to repository
                _alertRepository.Add(alert);

                // Create ViewModel
                var vm = new AlertCardViewModel(alert);

                // Add to active alerts
                var insertIndex = ActiveAlerts.ToList().FindIndex(a => a.Severity < alert.Severity);
                if (insertIndex < 0) insertIndex = ActiveAlerts.Count;
                ActiveAlerts.Insert(insertIndex, vm);
                _logger?.Debug("[MAINVM] Added to ActiveAlerts at index {Index}. Count: {Count}", insertIndex, ActiveAlerts.Count);
                Debug.WriteLine($"[MAINVM] Added to ActiveAlerts at index {insertIndex}. Count: {ActiveAlerts.Count}");

                // Add to history
                AlertHistory.Insert(0, vm);
                _logger?.Debug("[MAINVM] Added to AlertHistory. Count: {Count}", AlertHistory.Count);
                Debug.WriteLine($"[MAINVM] Added to AlertHistory. Count: {AlertHistory.Count}");

                // Trigger notifications
                if (plan.PlayAudio)
                {
                    _logger?.Debug("[MAINVM] Triggering audio notification");
                    Debug.WriteLine("[MAINVM] Triggering audio notification");
                    _audioService.Play(alert.Id, alert.Severity, plan.AudioVolume);
                }
                else
                {
                    _logger?.Debug("[MAINVM] Audio notification disabled in plan");
                    Debug.WriteLine("[MAINVM] Audio notification disabled in plan");
                }

                // Start escalation timer if acknowledgment required
                if (plan.RequireAcknowledgment)
                {
                    StartEscalationTimer(alert.Id, plan.AudioVolume);
                }

                if (plan.ShowOverlay && alert.Severity == AlertSeverity.Critical)
                {
                    _logger?.Debug("[MAINVM] Showing critical overlay");
                    Debug.WriteLine("[MAINVM] Showing critical overlay");
                    ShowOverlay = true;
                }

                _visualService.ShowAlert(alert, plan, () =>
                {
                    AcknowledgeAlertInternal(alert.Id);
                });

                // Bring window to front and focus
                BringWindowToFront();

                UpdateState();
                _logger?.Information("[MAINVM] Alert processed successfully: {AlertId}", alert.Id);
                Debug.WriteLine($"[MAINVM] Alert processed successfully: {alert.Id}");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "[MAINVM] Error processing received alert");
                Debug.WriteLine($"[MAINVM] Error processing received alert: {ex.Message}");
                Debug.WriteLine($"[MAINVM] Stack trace: {ex.StackTrace}");
            }
        });
    }

    private void OnAlertAcknowledged(AlertAcknowledgedEvent e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // Cancel escalation timer if exists
            if (_escalationTimers.TryRemove(e.AlertId, out var timer))
            {
                timer.Stop();
                _logger?.Debug("[MAINVM] Cancelled escalation timer for {AlertId}", e.AlertId);
                Debug.WriteLine($"[MAINVM] Cancelled escalation timer for {e.AlertId}");
            }

            // Stop any audio (initial notification and siren)
            _audioService.StopForAlert(e.AlertId);

            var vm = ActiveAlerts.FirstOrDefault(a => a.Id == e.AlertId);
            if (vm != null)
            {
                vm.MarkAcknowledged();
                ActiveAlerts.Remove(vm);
            }

            // Check if we should hide overlay
            if (!ActiveAlerts.Any(a => a.Severity == AlertSeverity.Critical))
            {
                ShowOverlay = false;
            }

            UpdateState();
        });
    }

    private void StartEscalationTimer(Guid alertId, float volume)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _escalationTimers.TryRemove(alertId, out _);
            _logger?.Information("[MAINVM] Alert {AlertId} not acknowledged within 5 seconds - playing siren", alertId);
            Debug.WriteLine($"[MAINVM] Alert {alertId} not acknowledged within 5 seconds - playing siren");
            _audioService.PlaySiren(alertId, 0.6f); // 60% volume for siren
        };
        _escalationTimers[alertId] = timer;
        timer.Start();
        _logger?.Debug("[MAINVM] Started escalation timer for {AlertId}", alertId);
        Debug.WriteLine($"[MAINVM] Started escalation timer for {alertId}");
    }

    private void OnListenerStatusChanged(ListenerStatusChangedEvent e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsListening = e.IsListening;
            ConnectionCount = e.ConnectionCount;

            if (e.IsListening)
            {
                StatusText = $"Listening on port {e.Port}";
            }
            else if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                StatusText = $"Error: {e.ErrorMessage}";
            }
            else
            {
                StatusText = "Stopped";
            }

            CommandManager.InvalidateRequerySuggested();
        });
    }

    // Command implementations
    private async void StartListening()
    {
        try
        {
            StatusText = $"Starting listener on port {Port}...";
            await _listenerService.StartAsync(Port);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to start: {ex.Message}";
        }
    }

    private async void StopListening()
    {
        await _listenerService.StopAsync();
        _audioService.Stop();
    }

    private void AcknowledgeAlert(object? parameter)
    {
        if (parameter is Guid alertId)
        {
            AcknowledgeAlertInternal(alertId);
        }
    }

    private void AcknowledgeAlertInternal(Guid alertId)
    {
        if (_alertRepository.Acknowledge(alertId))
        {
            var alert = _alertRepository.Get(alertId);
            if (alert != null)
            {
                _eventAggregator.Publish(new AlertAcknowledgedEvent(
                    alertId,
                    alert.AcknowledgedAt!.Value,
                    alert.ResponseTimeMs!.Value));
            }
        }
    }

    private void AcknowledgeAll()
    {
        foreach (var vm in ActiveAlerts.ToList())
        {
            AcknowledgeAlertInternal(vm.Id);
        }
    }

    private void ClearLog()
    {
        _alertRepository.ClearAcknowledged();
        AlertHistory.Clear();
        UpdateState();
    }

    private void ToggleAudio()
    {
        IsAudioEnabled = !IsAudioEnabled;
    }

    private void OpenSettings()
    {
        // Settings window is opened by the View
    }

    private void MinimizeToTray()
    {
        var window = Application.Current.MainWindow;
        if (window != null)
        {
            window.Hide();
        }
    }

    private void Exit()
    {
        Application.Current.Shutdown();
    }

    private void ApplyFilter()
    {
        // Filter implementation
        // In a real app, you'd use CollectionViewSource
    }

    private void UpdateState()
    {
        HasActiveAlerts = ActiveAlerts.Any();
        OnPropertyChanged(nameof(CriticalCount));
        OnPropertyChanged(nameof(HighCount));
        OnPropertyChanged(nameof(MediumCount));
    }

    public void Dispose()
    {
        _audioService.Dispose();
        _listenerService.Dispose();
    }

    private void BringWindowToFront()
    {
        var window = Application.Current.MainWindow;
        if (window == null)
            return;

        // Ensure window handle is created
        var helper = new System.Windows.Interop.WindowInteropHelper(window);
        var handle = helper.EnsureHandle();

        // Restore and show window
        ShowWindow(handle, SW_RESTORE);
        ShowWindow(handle, SW_SHOW);

        // Bring to front using WPF
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();

        // Force to front using Win32 API
        SwitchToThisWindow(handle, true);
        SetForegroundWindow(handle);
    }

    #region P/Invoke

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    #endregion
}
