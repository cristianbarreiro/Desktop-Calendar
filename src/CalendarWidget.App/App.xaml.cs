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
    private readonly ITrayService _trayService;
    private readonly IWindowPlacementService? _placementService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    /// <param name="lifetimeService">Application lifetime management service.</param>
    /// <param name="trayService">System tray management service.</param>
    /// <param name="placementService">Optional window placement service.</param>
    public App(
        IWindowManager windowManager,
        ApplicationLifetimeService lifetimeService,
        ITrayService trayService,
        IWindowPlacementService? placementService = null)
    {
        _windowManager = windowManager;
        _lifetimeService = lifetimeService;
        _trayService = trayService;
        _placementService = placementService;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayService.Initialize();

        // Show compact widget by default on startup
        _windowManager.ShowWidget();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        if (_placementService is not null)
        {
            try
            {
                _placementService.FlushPendingSaveAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore persistence exceptions on exit
            }
        }

        _trayService.Dispose();
        _lifetimeService.Shutdown(e.ApplicationExitCode);
        base.OnExit(e);
    }
}
