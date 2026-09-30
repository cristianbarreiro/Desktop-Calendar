using System.Windows;
using System.Windows.Input;
using CalendarWidget.App.Services;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.App.Windows;

/// <summary>
/// Interaction logic for the compact desktop calendar widget window.
/// </summary>
public partial class WidgetWindow : Window, IManagedWindow
{
    private readonly IWindowPlacementService? _placementService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the widget window.</param>
    /// <param name="placementService">Optional window placement service.</param>
    public WidgetWindow(WidgetViewModel viewModel, IWindowPlacementService? placementService = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _placementService = placementService;

        LocationChanged += OnLocationChanged;
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal && IsLoaded)
        {
            _placementService?.OnWidgetWindowBoundsChanged(Left, Top);
        }
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
