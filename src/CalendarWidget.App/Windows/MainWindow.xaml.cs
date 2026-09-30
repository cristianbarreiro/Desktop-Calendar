using System.Windows;
using CalendarWidget.App.Services;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.App.Windows;

/// <summary>
/// Interaction logic for the primary desktop application window.
/// </summary>
public partial class MainWindow : Window, IManagedWindow
{
    private readonly IWindowPlacementService? _placementService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the main window.</param>
    /// <param name="placementService">Optional window placement service.</param>
    public MainWindow(MainWindowViewModel viewModel, IWindowPlacementService? placementService = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _placementService = placementService;

        LocationChanged += OnLocationOrSizeChanged;
        SizeChanged += OnLocationOrSizeChanged;
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        LocationChanged -= OnLocationOrSizeChanged;
        SizeChanged -= OnLocationOrSizeChanged;
    }

    private void OnLocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal && IsLoaded)
        {
            double width = ActualWidth > 0 ? ActualWidth : Width;
            double height = ActualHeight > 0 ? ActualHeight : Height;
            _placementService?.OnMainWindowBoundsChanged(Left, Top, width, height);
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        Hide();
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
