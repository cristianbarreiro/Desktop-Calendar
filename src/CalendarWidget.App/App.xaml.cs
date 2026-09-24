using System.Windows;
using CalendarWidget.Presentation.Services;

namespace CalendarWidget.App;

/// <summary>
/// Interaction logic for App.xaml and application-level lifecycle events.
/// </summary>
public partial class App : Application
{
    private readonly IWindowManager _windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    public App(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Show compact widget by default on startup
        _windowManager.ShowWidget();
    }
}
