using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using winAlert.Services.Core;
using winAlert.Services.Data;
using winAlert.Services.Network;
using winAlert.Services.Notification;
using winAlert.ViewModels;
using winAlert.Views;

namespace winAlert;

/// <summary>
/// Application entry point with DI configuration.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure logging first
        ConfigureLogging();

        // Configure MaterialDesign theme
        ConfigureMaterialDesignTheme();

        _logger?.Information("winAlert starting up...");

        try
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Set up global exception handling
            SetupExceptionHandling();

            // Create and show main window
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var mainWindow = new MainWindow(mainViewModel);
            MainWindow = mainWindow;

            // Handle minimize to tray
            mainWindow.StateChanged += OnMainWindowStateChanged;

            // Load settings and apply behavior
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

            mainWindow.Show();

            // Start listening automatically
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
            MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Information("winAlert shutting down...");

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void ConfigureMaterialDesignTheme()
    {
        try
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            // Set base theme to Dark
            theme.SetBaseTheme(BaseTheme.Dark);

            // Configure primary color (Teal) - using pre-calculated light/dark variants
            var primaryColor = (Color)ColorConverter.ConvertFromString("#009688");
            var primaryLight = (Color)ColorConverter.ConvertFromString("#4DB6AC");
            var primaryDark = (Color)ColorConverter.ConvertFromString("#00796B");
            theme.PrimaryLight = new ColorPair(primaryLight, Colors.Black);
            theme.PrimaryMid = new ColorPair(primaryColor, Colors.White);
            theme.PrimaryDark = new ColorPair(primaryDark, Colors.White);

            // Configure secondary color (Amber) - using pre-calculated light/dark variants
            var secondaryColor = (Color)ColorConverter.ConvertFromString("#FFC107");
            var secondaryLight = (Color)ColorConverter.ConvertFromString("#FFD54F");
            var secondaryDark = (Color)ColorConverter.ConvertFromString("#FFA000");
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
        // Register Serilog
        services.AddSingleton<ILogger>(Log.Logger);

        // Register core services
        services.AddSingleton<IEventAggregator, EventAggregator>();

        // Register data services
        services.AddSingleton<IAlertRepository, AlertRepository>();
        services.AddSingleton<ISettingsManager, SettingsManager>();

        // Register network services
        services.AddSingleton<IAlertParser, AlertParser>();
        services.AddSingleton<IAlertListenerService, AlertListenerService>();

        // Register notification services
        services.AddSingleton<IAlertTriageEngine, AlertTriageEngine>();
        services.AddSingleton<IAudioNotificationService, AudioNotificationService>();
        services.AddSingleton<IVisualNotificationService, VisualNotificationService>();

        // Register ViewModels
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
                MessageBox.Show($"A fatal error occurred: {exception?.Message}\n\nThe application will now close.",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.Error(args.Exception, "Unhandled dispatcher exception");

            MessageBox.Show($"An error occurred: {args.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
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
}
