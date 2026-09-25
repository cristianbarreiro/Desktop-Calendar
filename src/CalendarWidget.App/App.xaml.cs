using System.Windows;
using CalendarWidget.App.Services;
using CalendarWidget.Presentation.Services;

namespace CalendarWidget.App;

/// <summary>
/// Interaction logic for App.xaml and application-level lifecycle events.
/// </summary>
public partial class App : Application
{
    private readonly IWindowManager _windowManager;
    private readonly ApplicationLifetimeService _lifetimeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    /// <param name="lifetimeService">Application lifetime management service.</param>
    public App(IWindowManager windowManager, ApplicationLifetimeService lifetimeService)
    {
        _windowManager = windowManager;
        _lifetimeService = lifetimeService;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Show compact widget by default on startup
        _windowManager.ShowWidget();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _lifetimeService.Shutdown(e.ApplicationExitCode);
        base.OnExit(e);
    }
}
