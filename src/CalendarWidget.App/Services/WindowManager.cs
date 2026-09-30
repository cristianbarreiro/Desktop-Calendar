using System.Windows;
using CalendarWidget.App.Windows;
using CalendarWidget.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.App.Services;

/// <summary>
/// Orchestrates window display, switching, visibility states, and lifecycle between Widget and Full Application views.
/// </summary>
public sealed class WindowManager : IWindowManager
{
    private readonly Func<IManagedWindow> _mainWindowFactory;
    private readonly Func<IManagedWindow> _widgetWindowFactory;
    private readonly ApplicationLifetimeService _lifetimeService;
    private readonly IWindowPlacementService? _placementService;
    private IManagedWindow? _mainWindow;
    private IManagedWindow? _widgetWindow;
    private bool _isSwitching;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class using the service provider.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection service provider.</param>
    /// <param name="lifetimeService">Application lifetime coordination service.</param>
    /// <param name="placementService">Optional window placement service.</param>
    public WindowManager(
        IServiceProvider serviceProvider,
        ApplicationLifetimeService lifetimeService,
        IWindowPlacementService? placementService = null)
        : this(
            () => serviceProvider.GetRequiredService<MainWindow>(),
            () => serviceProvider.GetRequiredService<WidgetWindow>(),
            lifetimeService,
            placementService ?? serviceProvider.GetService<IWindowPlacementService>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class using explicit factories.
    /// </summary>
    /// <param name="mainWindowFactory">Factory creating the main application window.</param>
    /// <param name="widgetWindowFactory">Factory creating the widget window.</param>
    /// <param name="lifetimeService">Application lifetime coordination service.</param>
    /// <param name="placementService">Optional window placement service.</param>
    public WindowManager(
        Func<IManagedWindow> mainWindowFactory,
        Func<IManagedWindow> widgetWindowFactory,
        ApplicationLifetimeService lifetimeService,
        IWindowPlacementService? placementService = null)
    {
        _mainWindowFactory = mainWindowFactory;
        _widgetWindowFactory = widgetWindowFactory;
        _lifetimeService = lifetimeService;
        _placementService = placementService;
    }

    /// <inheritdoc />
    public bool IsFullApplicationVisible => _mainWindow is not null && _mainWindow.IsVisible;

    /// <inheritdoc />
    public bool IsWidgetVisible => _widgetWindow is not null && _widgetWindow.IsVisible;

    /// <inheritdoc />
    public void ActivateCurrentWindow()
    {
        ExecuteOnDispatcher(() =>
        {
            if (IsFullApplicationVisible && _mainWindow is not null)
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Show();
                _mainWindow.Activate();
            }
            else
            {
                ShowWidget();
            }
        });
    }

    /// <inheritdoc />
    public void ShowFullApplication()
    {
        ExecuteOnDispatcher(() =>
        {
            _isSwitching = true;
            try
            {
                if (_widgetWindow is not null && _widgetWindow.IsVisible)
                {
                    _widgetWindow.Hide();
                }

                if (_mainWindow is null)
                {
                    _mainWindow = _mainWindowFactory();
                    _placementService?.ApplyMainWindowBounds(_mainWindow);
                    _mainWindow.Closed += OnMainWindowClosed;
                }

                _mainWindow.Show();
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Activate();
            }
            finally
            {
                _isSwitching = false;
            }
        });
    }

    /// <inheritdoc />
    public void ShowWidget()
    {
        ExecuteOnDispatcher(() =>
        {
            _isSwitching = true;
            try
            {
                if (_mainWindow is not null && _mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                }

                if (_widgetWindow is null)
                {
                    _widgetWindow = _widgetWindowFactory();
                    _placementService?.ApplyWidgetWindowBounds(_widgetWindow);
                    _widgetWindow.Closed += OnWidgetWindowClosed;
                }

                _widgetWindow.Show();
                if (_widgetWindow.WindowState == WindowState.Minimized)
                {
                    _widgetWindow.WindowState = WindowState.Normal;
                }
                _widgetWindow.Activate();
            }
            finally
            {
                _isSwitching = false;
            }
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
                _widgetWindow.Hide();
            }
        });
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.Closed -= OnMainWindowClosed;
            _mainWindow = null;
        }

        if (!_isSwitching)
        {
            _lifetimeService.Shutdown();
        }
    }

    private void OnWidgetWindowClosed(object? sender, EventArgs e)
    {
        if (_widgetWindow is not null)
        {
            _widgetWindow.Closed -= OnWidgetWindowClosed;
            _widgetWindow = null;
        }

        if (!_isSwitching)
        {
            _lifetimeService.Shutdown();
        }
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
