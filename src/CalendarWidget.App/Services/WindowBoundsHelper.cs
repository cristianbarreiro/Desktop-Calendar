namespace CalendarWidget.App.Services;

/// <summary>
/// Provides deterministic validation and off-screen recovery logic for window coordinates across multiple monitors.
/// </summary>
public static class WindowBoundsHelper
{
    private const double MinVisibleDimension = 50.0;

    /// <summary>
    /// Validates whether the given window bounds are sufficiently visible on any active display area.
    /// If off-screen or on a disconnected monitor, returns recovered bounds centered on the default display.
    /// </summary>
    /// <param name="requestedBounds">The saved or requested window bounds.</param>
    /// <param name="displayAreas">The list of currently active monitor working areas.</param>
    /// <param name="defaultDisplay">The primary fallback display area.</param>
    /// <returns>Usable window bounds guaranteed to be reachable on an active display.</returns>
    public static WindowBounds EnsureVisible(
        WindowBounds requestedBounds,
        IReadOnlyList<DisplayArea> displayAreas,
        DisplayArea defaultDisplay)
    {
        double width = requestedBounds.Width > 0 && !double.IsNaN(requestedBounds.Width) && !double.IsInfinity(requestedBounds.Width)
            ? requestedBounds.Width
            : 800;
        double height = requestedBounds.Height > 0 && !double.IsNaN(requestedBounds.Height) && !double.IsInfinity(requestedBounds.Height)
            ? requestedBounds.Height
            : 600;

        if (double.IsNaN(requestedBounds.Left) || double.IsInfinity(requestedBounds.Left) ||
            double.IsNaN(requestedBounds.Top) || double.IsInfinity(requestedBounds.Top) ||
            displayAreas.Count == 0)
        {
            return CenterOnDisplay(width, height, defaultDisplay);
        }

        foreach (DisplayArea display in displayAreas)
        {
            if (display.Width <= 0 || display.Height <= 0 ||
                double.IsNaN(display.Left) || double.IsNaN(display.Top))
            {
                continue;
            }

            double intersectLeft = Math.Max(requestedBounds.Left, display.Left);
            double intersectTop = Math.Max(requestedBounds.Top, display.Top);
            double intersectRight = Math.Min(requestedBounds.Right, display.Right);
            double intersectBottom = Math.Min(requestedBounds.Bottom, display.Bottom);

            double intersectWidth = Math.Max(0, intersectRight - intersectLeft);
            double intersectHeight = Math.Max(0, intersectBottom - intersectTop);

            if (intersectWidth >= MinVisibleDimension && intersectHeight >= MinVisibleDimension)
            {
                // The window is sufficiently visible on this active display.
                // Ensure the top edge is not hidden above the top or below the bottom of this monitor.
                double safeTop = Math.Max(requestedBounds.Top, display.Top);
                safeTop = Math.Min(safeTop, Math.Max(display.Top, display.Bottom - MinVisibleDimension));
                return new WindowBounds(requestedBounds.Left, safeTop, width, height);
            }
        }

        // Window is outside all active displays (e.g. saved at -5000, -5000 or disconnected monitor).
        // Recover onto the default display center.
        return CenterOnDisplay(width, height, defaultDisplay);
    }

    /// <summary>
    /// Centers dimensions on the specified display area.
    /// </summary>
    public static WindowBounds CenterOnDisplay(double width, double height, DisplayArea display)
    {
        double safeWidth = width > 0 && !double.IsNaN(width) && !double.IsInfinity(width) ? width : 800;
        double safeHeight = height > 0 && !double.IsNaN(height) && !double.IsInfinity(height) ? height : 600;

        double dispWidth = display.Width > 0 && !double.IsNaN(display.Width) && !double.IsInfinity(display.Width) ? display.Width : safeWidth;
        double dispHeight = display.Height > 0 && !double.IsNaN(display.Height) && !double.IsInfinity(display.Height) ? display.Height : safeHeight;
        double dispLeft = double.IsNaN(display.Left) || double.IsInfinity(display.Left) ? 0 : display.Left;
        double dispTop = double.IsNaN(display.Top) || double.IsInfinity(display.Top) ? 0 : display.Top;

        double actualWidth = Math.Min(safeWidth, dispWidth);
        double actualHeight = Math.Min(safeHeight, dispHeight);

        double left = dispLeft + Math.Max(0, (dispWidth - actualWidth) / 2);
        double top = dispTop + Math.Max(0, (dispHeight - actualHeight) / 2);

        return new WindowBounds(left, top, actualWidth, actualHeight);
    }
}
