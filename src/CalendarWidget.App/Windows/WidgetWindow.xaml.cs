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
    private bool _isConstrainingBounds;

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
        SizeChanged += OnSizeChanged;
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        LocationChanged -= OnLocationChanged;
        SizeChanged -= OnSizeChanged;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        ConstrainToWorkingArea(ActualWidth, ActualHeight);

        if (WindowState == WindowState.Normal && IsLoaded)
        {
            _placementService?.OnWidgetWindowBoundsChanged(Left, Top);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ConstrainToWorkingArea(e.NewSize.Width, e.NewSize.Height);
    }

    private void ConstrainToWorkingArea(double width, double height)
    {
        if (_placementService is null || _isConstrainingBounds || width <= 0 || height <= 0)
        {
            return;
        }

        WindowBounds bounded = _placementService.EnsureWidgetWindowVisible(new WindowBounds(Left, Top, width, height));
        _isConstrainingBounds = true;
        try
        {
            Left = bounded.Left;
            Top = bounded.Top;
            if (bounded.Width < width)
            {
                Width = bounded.Width;
            }
            if (bounded.Height < height)
            {
                MinHeight = bounded.Height;
                Height = bounded.Height;
            }
        }
        finally
        {
            _isConstrainingBounds = false;
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
