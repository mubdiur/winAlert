using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using winAlert.Notifications;
using winAlert.Services.Core;
using winAlert.Services.Data;
using winAlert.Services.Network;
using winAlert.Services.Notification;
using winAlert.ViewModels;
using winAlert.Views;

namespace winAlert;

/// <summary>
/// Application entry point with DI configuration, single instance enforcement, and system tray support.
/// </summary>
public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "winAlert_SingleInstance_Mutex";
    private static Mutex? _singleInstanceMutex;
    private static bool _mutexAcquired;
    
    private IServiceProvider? _serviceProvider;
    public static IServiceProvider? ServiceProvider { get; private set; }
    private ILogger? _logger;
    private TrayIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        // ENFORCE SINGLE INSTANCE
        if (!EnsureSingleInstance())
        {
            ActivateExistingInstance();
            Shutdown(0);
            return;
        }

        base.OnStartup(e);

        ConfigureLogging();
        ConfigureMaterialDesignTheme();

        _logger?.Information("winAlert starting up...");

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            ServiceProvider = _serviceProvider;

            SetupExceptionHandling();
            InitializeSystemTray();

            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Closing += OnMainWindowClosing;
            MainWindow = mainWindow;

            mainWindow.StateChanged += OnMainWindowStateChanged;

            var settingsManager = _serviceProvider.GetRequiredService<ISettingsManager>();
            var settings = settingsManager.CurrentSettings;

            if (settings.Behavior.AlwaysOnTop)
            {
                mainWindow.Topmost = true;
            }

            if (settings.Behavior.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                if (settings.Behavior.MinimizeToTray)
                {
                    mainWindow.Hide();
                }
            }
            else
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Show();

            if (settings.Network.Port > 0)
            {
                var listener = _serviceProvider.GetRequiredService<IAlertListenerService>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await listener.StartAsync(settings.Network.Port);
                        _logger?.Information("Started listening on port {Port}", settings.Network.Port);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex, "Failed to start listener on port {Port}", settings.Network.Port);
                    }
                });
            }
            else
            {
                _logger?.Warning("No port configured, listener not started");
            }

            _logger?.Information("winAlert started successfully");
        }
        catch (Exception ex)
        {
            _logger?.Fatal(ex, "Application startup failed");
            System.Windows.MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Information("winAlert shutting down...");

        if (_trayIcon != null)
        {
            _trayIcon.ShowWindowRequested -= ShowMainWindow;
            _trayIcon.ExitRequested -= ExitApplication;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_singleInstanceMutex != null)
        {
            if (_mutexAcquired)
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static bool EnsureSingleInstance()
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
        _mutexAcquired = createdNew;
        return createdNew;
    }

    private static void ActivateExistingInstance()
    {
        try
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    NativeMethods.SwitchToThisWindow(process.MainWindowHandle, true);
                    break;
                }
            }
        }
        catch
        {
        }
    }

    private void InitializeSystemTray()
    {
        _logger?.Information("Initializing system tray icon...");
        
        _trayIcon = new TrayIcon();
        _trayIcon.ShowWindowRequested += ShowMainWindow;
        _trayIcon.ExitRequested += ExitApplication;
        _trayIcon.Initialize("winAlert - Alert Monitor");
        
        _logger?.Information("System tray icon initialized successfully");
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
        
        var helper = new System.Windows.Interop.WindowInteropHelper(MainWindow);
        NativeMethods.SetForegroundWindow(helper.Handle);
        NativeMethods.SwitchToThisWindow(helper.Handle, true);
    }

    private void ExitApplication()
    {
        _logger?.Information("Exit requested from tray menu");
        Shutdown(0);
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is Window window)
        {
            e.Cancel = true;
            window.Hide();
            _logger?.Debug("Main window close intercepted - minimized to tray");
        }
    }

    private void OnMainWindowStateChanged(object? sender, EventArgs e)
    {
        if (MainWindow?.WindowState == WindowState.Minimized)
        {
            var settingsManager = _serviceProvider?.GetService<ISettingsManager>();
            if (settingsManager?.CurrentSettings.Behavior.MinimizeToTray == true)
            {
                MainWindow?.Hide();
            }
        }
    }

    private void ConfigureMaterialDesignTheme()
    {
        try
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(BaseTheme.Dark);

            var primaryColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#009688");
            var primaryLight = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4DB6AC");
            var primaryDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00796B");
            theme.PrimaryLight = new ColorPair(primaryLight, Colors.Black);
            theme.PrimaryMid = new ColorPair(primaryColor, Colors.White);
            theme.PrimaryDark = new ColorPair(primaryDark, Colors.White);

            var secondaryColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFC107");
            var secondaryLight = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD54F");
            var secondaryDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFA000");
            theme.SecondaryLight = new ColorPair(secondaryLight, Colors.Black);
            theme.SecondaryMid = new ColorPair(secondaryColor, Colors.Black);
            theme.SecondaryDark = new ColorPair(secondaryDark, Colors.Black);

            paletteHelper.SetTheme(theme);

            _logger?.Information("MaterialDesign theme configured: Dark mode with Teal primary, Amber secondary");
        }
        catch (Exception ex)
        {
            _logger?.Warning(ex, "Failed to configure MaterialDesign theme programmatically");
        }
    }

    private void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "winAlert",
            "logs",
            "winalert-.log");

        var logDirectory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _logger = Log.Logger;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger>(Log.Logger);
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<IAlertRepository, AlertRepository>();
        services.AddSingleton<ISettingsManager, SettingsManager>();
        services.AddSingleton<IAlertParser, AlertParser>();
        services.AddSingleton<IAlertListenerService, AlertListenerService>();
        services.AddSingleton<IAlertTriageEngine, AlertTriageEngine>();
        services.AddSingleton<IAudioNotificationService, AudioNotificationService>();
        services.AddSingleton<IVisualNotificationService, VisualNotificationService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            _logger?.Fatal(exception, "Unhandled domain exception");

            if (args.IsTerminating)
            {
                System.Windows.MessageBox.Show($"A fatal error occurred: {exception?.Message}\n\nThe application will now close.",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.Error(args.Exception, "Unhandled dispatcher exception");
            System.Windows.MessageBox.Show($"An error occurred: {args.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
}
