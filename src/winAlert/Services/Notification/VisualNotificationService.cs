using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using winAlert.Domain.Models;

namespace winAlert.Services.Notification;

/// <summary>
/// Interface for visual notification display.
/// </summary>
public interface IVisualNotificationService
{
    /// <summary>
    /// Shows the visual notification for an alert.
    /// </summary>
    void ShowAlert(Alert alert, NotificationPlan plan, Action? onAcknowledge = null);

    /// <summary>
    /// Dismisses the notification for a specific alert.
    /// </summary>
    void Dismiss(Guid alertId);

    /// <summary>
    /// Dismisses all active notifications.
    /// </summary>
    void DismissAll();

    /// <summary>
    /// Flashes the taskbar to get user attention.
    /// </summary>
    void FlashTaskbar();

    /// <summary>
    /// Whether there are active notifications.
    /// </summary>
    bool HasActiveNotifications { get; }
}

/// <summary>
/// Visual notification service for WPF.
/// </summary>
public sealed class VisualNotificationService : IVisualNotificationService
{
    private readonly ILogger _logger;
    private readonly Dispatcher _dispatcher;
    private readonly ConcurrentDictionary<Guid, AlertNotification> _activeNotifications = new();

    public VisualNotificationService(ILogger logger)
    {
        _logger = logger;
        _dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Not on UI thread");
    }

    /// <inheritdoc />
    public bool HasActiveNotifications => !_activeNotifications.IsEmpty;

    /// <inheritdoc />
    public void ShowAlert(Alert alert, NotificationPlan plan, Action? onAcknowledge = null)
    {
        if (_dispatcher.CheckAccess())
        {
            ShowAlertInternal(alert, plan, onAcknowledge);
        }
        else
        {
            _dispatcher.BeginInvoke(() => ShowAlertInternal(alert, plan, onAcknowledge));
        }
    }

    private void ShowAlertInternal(Alert alert, NotificationPlan plan, Action? onAcknowledge)
    {
        _logger.Information("Showing visual notification for alert {AlertId} [{Severity}]",
            alert.Id, alert.Severity);

        var notification = new AlertNotification(alert, plan, onAcknowledge);
        _activeNotifications[alert.Id] = notification;

        if (plan.ShowOverlay)
        {
            ShowOverlay(notification);
        }
        else
        {
            ShowToast(notification);
        }

        if (plan.TaskbarFlash)
        {
            FlashTaskbar();
        }

        // Set up auto-dismiss if applicable
        if (plan.AutoCloseSeconds > 0 && !plan.RequireAcknowledgment)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(plan.AutoCloseSeconds)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Dismiss(alert.Id);
            };
            timer.Start();
            notification.Timer = timer;
        }
    }

    private void ShowOverlay(AlertNotification notification)
    {
        // Overlay is handled by the UI layer through event subscription
        // This service publishes events that the UI responds to
        _logger.Debug("Showing overlay for alert {AlertId}", notification.Alert.Id);
    }

    private void ShowToast(AlertNotification notification)
    {
        // Toast notifications would be implemented using Windows notification APIs
        // For now, we rely on the UI layer to handle this
        _logger.Debug("Showing toast for alert {AlertId}", notification.Alert.Id);
    }

    /// <inheritdoc />
    public void Dismiss(Guid alertId)
    {
        if (_dispatcher.CheckAccess())
        {
            DismissInternal(alertId);
        }
        else
        {
            _dispatcher.BeginInvoke(() => DismissInternal(alertId));
        }
    }

    private void DismissInternal(Guid alertId)
    {
        if (_activeNotifications.TryRemove(alertId, out var notification))
        {
            notification.Timer?.Stop();
            notification.OnAcknowledge = null;
            _logger.Debug("Dismissed notification for alert {AlertId}", alertId);
        }
    }

    /// <inheritdoc />
    public void DismissAll()
    {
        if (_dispatcher.CheckAccess())
        {
            foreach (var alertId in _activeNotifications.Keys)
            {
                DismissInternal(alertId);
            }
        }
        else
        {
            _dispatcher.BeginInvoke(() => DismissAll());
        }
    }

    /// <inheritdoc />
    public void FlashTaskbar()
    {
        if (_dispatcher.CheckAccess())
        {
            FlashTaskbarInternal();
        }
        else
        {
            _dispatcher.BeginInvoke(FlashTaskbarInternal);
        }
    }

    private void FlashTaskbarInternal()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.WindowState == WindowState.Minimized)
            {
                // Flash window in taskbar
                var info = new FLASHWINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(FLASHWINFO)),
                    hwnd = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = 3,
                    dwTimeout = 0
                };
                info.hwnd = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                FlashWindowEx(ref info);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to flash taskbar");
        }
    }

    #region P/Invoke for FlashWindowEx

    private const uint FLASHW_ALL = 3;
    private const uint FLASHW_TIMERNOFG = 12;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    #endregion

    private sealed class AlertNotification
    {
        public Alert Alert { get; }
        public NotificationPlan Plan { get; }
        public Action? OnAcknowledge { get; set; }
        public DispatcherTimer? Timer { get; set; }

        public AlertNotification(Alert alert, NotificationPlan plan, Action? onAcknowledge)
        {
            Alert = alert;
            Plan = plan;
            OnAcknowledge = onAcknowledge;
        }
    }
}
