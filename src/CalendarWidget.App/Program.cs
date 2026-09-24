using CalendarWidget.App.Services;
using CalendarWidget.App.Windows;
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
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                // Application Lifecycle and Window Management
                services.AddSingleton<App>();
                services.AddSingleton<IWindowManager, WindowManager>();
                services.AddSingleton<ApplicationLifetimeService>();

                // Presentation Services
                services.AddSingleton<IClockService, SystemClockService>();
                services.AddSingleton<ICalendarGridService, CalendarGridService>();

                // Presentation ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<WidgetViewModel>();
                services.AddTransient<CalendarViewModel>();
                services.AddTransient<NotesViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Windows
                services.AddTransient<MainWindow>();
                services.AddTransient<WidgetWindow>();
            })
            .Build();

        host.Start();

        App app = host.Services.GetRequiredService<App>();
        app.InitializeComponent();
        app.Run();

        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
