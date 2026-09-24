using System.Windows;
using CalendarWidget.App.Windows;
using CalendarWidget.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.App.Services;

/// <summary>
/// Orchestrates window display, switching, and visibility states between Widget and Full Application views.
/// </summary>
public sealed class WindowManager : IWindowManager
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _mainWindow;
    private Window? _widgetWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection service provider.</param>
    public WindowManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public bool IsFullApplicationVisible => _mainWindow is not null && _mainWindow.IsVisible;

    /// <inheritdoc />
    public bool IsWidgetVisible => _widgetWindow is not null && _widgetWindow.IsVisible;

    /// <inheritdoc />
    public void ShowFullApplication()
    {
        ExecuteOnDispatcher(() =>
        {
            if (_widgetWindow is not null && _widgetWindow.IsVisible)
            {
                _widgetWindow.Hide();
            }

            if (_mainWindow is null)
            {
                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                _mainWindow.Closed += (_, _) => _mainWindow = null;
            }

            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        });
    }

    /// <inheritdoc />
    public void ShowWidget()
    {
        ExecuteOnDispatcher(() =>
        {
            if (_mainWindow is not null && _mainWindow.IsVisible)
            {
                _mainWindow.Hide();
            }

            if (_widgetWindow is null)
            {
                _widgetWindow = _serviceProvider.GetRequiredService<WidgetWindow>();
                _widgetWindow.Closed += (_, _) => _widgetWindow = null;
            }

            _widgetWindow.Show();
            if (_widgetWindow.WindowState == WindowState.Minimized)
            {
                _widgetWindow.WindowState = WindowState.Normal;
            }
            _widgetWindow.Activate();
        });
    }

    /// <inheritdoc />
    public void MinimizeWidget()
    {
        ExecuteOnDispatcher(() =>
        {
            if (_widgetWindow is not null)
            {
                _widgetWindow.WindowState = WindowState.Minimized;
            }
        });
    }

    private static void ExecuteOnDispatcher(Action action)
    {
        if (Application.Current is null)
        {
            action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}
