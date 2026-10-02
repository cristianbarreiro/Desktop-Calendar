using System.IO;
using CalendarWidget.App.Services;
using CalendarWidget.App.Windows;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Infrastructure;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CalendarWidget.App;

/// <summary>
/// Application entry point and Generic Host composition root.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application main entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopCalendar");

        Directory.CreateDirectory(appDataDir);

        string dbPath = Path.Combine(appDataDir, "calendar.db");
        string settingsPath = Path.Combine(appDataDir, "settings.json");
        string connectionString = $"Data Source={dbPath}";

        using SingleInstanceCoordinator singleInstance = new();
        if (!singleInstance.IsPrimary)
        {
            singleInstance.SignalPrimary();
            return;
        }

        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                // Infrastructure (persistence, repositories, settings, OS integrations)
                services.AddInfrastructure(connectionString, settingsPath);

                // Application Lifecycle, Shell Integration, and Window Management
                services.AddSingleton<ISingleInstanceCoordinator>(singleInstance);
                services.AddSingleton<IDisplayMonitorProvider, WpfDisplayMonitorProvider>();
                services.AddSingleton<IWindowPlacementService, WindowPlacementService>();
                services.AddSingleton<ITrayService, SystemTrayService>();
                services.AddSingleton<App>();
                services.AddSingleton<IWindowManager, WindowManager>();
                services.AddSingleton<ApplicationLifetimeService>();

                // Presentation Services
                services.AddSingleton<IClockService, SystemClockService>();
                services.AddSingleton<ICalendarGridService, CalendarGridService>();
                services.AddSingleton<ISystemThemeDetector, SystemThemeDetector>();
                services.AddSingleton<IThemeService, WpfThemeService>();
                services.AddSingleton<IDateTimeFormatService, DateTimeFormatService>();
                services.AddSingleton<IFileDialogService, WpfFileDialogService>();

                // Presentation ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<WidgetViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<CalendarViewModel>();
                services.AddTransient<NotesViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Windows
                services.AddTransient<MainWindow>();
                services.AddTransient<WidgetWindow>();
            })
            .Build();

        host.Start();

        IWindowManager windowManager = host.Services.GetRequiredService<IWindowManager>();
        singleInstance.SetActivationHandler(() => windowManager.ActivateCurrentWindow());
        singleInstance.StartListening();

        // Initialize database (apply migrations, enable WAL) and application settings
        using (IServiceScope scope = host.Services.CreateScope())
        {
            DatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            initializer.InitializeAsync().GetAwaiter().GetResult();

            ISettingsService settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            settingsService.InitializeAsync().GetAwaiter().GetResult();

            IThemeService themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();
            themeService.ApplyTheme(settingsService.CurrentSettings.Theme);
        }

        App app = host.Services.GetRequiredService<App>();
        app.InitializeComponent();
        app.Run();

        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
