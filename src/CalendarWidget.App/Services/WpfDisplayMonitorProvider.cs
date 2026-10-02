using System.Runtime.InteropServices;
using System.Windows;
using FormsScreen = System.Windows.Forms.Screen;

namespace CalendarWidget.App.Services;

/// <summary>
/// Provides display monitor bounds using Windows desktop screen APIs.
/// </summary>
public sealed class WpfDisplayMonitorProvider : IDisplayMonitorProvider
{
    /// <inheritdoc />
    public IReadOnlyList<DisplayArea> GetDisplayAreas()
    {
        try
        {
            FormsScreen[] screens = FormsScreen.AllScreens;
            if (screens.Length > 0)
            {
                var areas = new List<DisplayArea>(screens.Length);
                foreach (FormsScreen screen in screens)
                {
                    areas.Add(ToDisplayArea(screen));
                }

                return areas;
            }
        }
        catch
        {
            // Fallback to WPF SystemParameters if Screen.AllScreens fails
        }

        Rect wpfWorkArea = SystemParameters.WorkArea;
        return [new DisplayArea(wpfWorkArea.Left, wpfWorkArea.Top, wpfWorkArea.Width, wpfWorkArea.Height)];
    }

    /// <inheritdoc />
    public DisplayArea GetPrimaryDisplayArea()
    {
        try
        {
            FormsScreen? primary = FormsScreen.PrimaryScreen;
            if (primary is not null)
            {
                return ToDisplayArea(primary);
            }
        }
        catch
        {
            // Fallback to WPF SystemParameters
        }

        Rect wpfWorkArea = SystemParameters.WorkArea;
        return new DisplayArea(wpfWorkArea.Left, wpfWorkArea.Top, wpfWorkArea.Width, wpfWorkArea.Height);
    }

    private static DisplayArea ToDisplayArea(FormsScreen screen)
    {
        System.Drawing.Rectangle workingArea = screen.WorkingArea;
        NativePoint center = new(
            workingArea.Left + (workingArea.Width / 2),
            workingArea.Top + (workingArea.Height / 2));
        nint monitor = MonitorFromPoint(center, MonitorDefaultToNearest);

        if (monitor != 0 && GetDpiForMonitor(monitor, MonitorDpiType.Effective, out uint dpiX, out uint dpiY) == 0)
        {
            return DisplayArea.FromDevicePixels(
                workingArea.Left,
                workingArea.Top,
                workingArea.Width,
                workingArea.Height,
                dpiX,
                dpiY);
        }

        return DisplayArea.FromDevicePixels(
            workingArea.Left,
            workingArea.Top,
            workingArea.Width,
            workingArea.Height,
            96,
            96);
    }

    private const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    private enum MonitorDpiType
    {
        Effective
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }
}
