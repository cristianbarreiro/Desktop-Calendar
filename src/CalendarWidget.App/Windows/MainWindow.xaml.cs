using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CalendarWidget.App.Services;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.App.Windows;

/// <summary>
/// Interaction logic for the primary desktop application window.
/// </summary>
public partial class MainWindow : Window, IManagedWindow
{
    private readonly IWindowPlacementService? _placementService;
    private HwndSource? _windowSource;

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
        StateChanged += OnWindowStateChanged;
        SourceInitialized += OnSourceInitialized;
        Closed += OnWindowClosed;
        UpdateMaximizeRestoreControl();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        SourceInitialized -= OnSourceInitialized;
        LocationChanged -= OnLocationOrSizeChanged;
        SizeChanged -= OnLocationOrSizeChanged;
        StateChanged -= OnWindowStateChanged;
        _windowSource?.RemoveHook(OnWindowMessage);
        _windowSource = null;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowSource = (HwndSource?)PresentationSource.FromVisual(this);
        _windowSource?.AddHook(OnWindowMessage);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is WmDpiChanged or WmSettingChange)
        {
            QueueMaximizedViewportAdjustment();
        }
        return IntPtr.Zero;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeRestoreControl();
        QueueMaximizedViewportAdjustment();
    }

    private void QueueMaximizedViewportAdjustment()
    {
        if (!IsLoaded)
        {
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, UpdateMaximizedViewport);
    }

    private void UpdateMaximizedViewport()
    {
        if (WindowState != WindowState.Maximized || !IsVisible || _windowSource is null)
        {
            MainWindowRootGrid.Margin = default;
            return;
        }

        IntPtr monitor = MonitorFromWindow(_windowSource.Handle, MonitorDefaultToNearest);
        NativeMonitorInfo monitorInfo = new() { Size = Marshal.SizeOf<NativeMonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(MainWindowRootGrid);
        Thickness margin = MainWindowRootGrid.Margin;
        System.Windows.Point topLeft = MainWindowRootGrid.PointToScreen(new System.Windows.Point());
        System.Windows.Point bottomRight = MainWindowRootGrid.PointToScreen(
            new System.Windows.Point(MainWindowRootGrid.ActualWidth, MainWindowRootGrid.ActualHeight));
        double unadjustedLeft = topLeft.X - margin.Left * dpi.DpiScaleX;
        double unadjustedTop = topLeft.Y - margin.Top * dpi.DpiScaleY;
        double unadjustedRight = bottomRight.X + margin.Right * dpi.DpiScaleX;
        double unadjustedBottom = bottomRight.Y + margin.Bottom * dpi.DpiScaleY;
        NativeRect work = monitorInfo.Work;

        MainWindowRootGrid.Margin = new Thickness(
            Math.Max(0, work.Left - unadjustedLeft) / dpi.DpiScaleX,
            Math.Max(0, work.Top - unadjustedTop) / dpi.DpiScaleY,
            Math.Max(0, unadjustedRight - work.Right) / dpi.DpiScaleX,
            Math.Max(0, unadjustedBottom - work.Bottom) / dpi.DpiScaleY);
    }

    private void UpdateMaximizeRestoreControl()
    {
        bool isMaximized = WindowState == WindowState.Maximized;
        string actionName = isMaximized ? "Restore" : "Maximize";

        MaximizeRestoreIcon.Data = Geometry.Parse(isMaximized
            ? "M 5,2 H 12 V 9 M 10,5 H 2 V 12 H 10 Z"
            : "M 2,2 H 12 V 12 H 2 Z");
        AutomationProperties.SetName(MaximizeRestoreButton, actionName);
        MaximizeRestoreButton.ToolTip = actionName;
    }

    private void OnLocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            QueueMaximizedViewportAdjustment();
        }

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

    private const int WmSettingChange = 0x001A;
    private const int WmDpiChanged = 0x02E0;
    private const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref NativeMonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

}
