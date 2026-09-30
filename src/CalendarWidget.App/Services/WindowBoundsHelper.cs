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
        double width = requestedBounds.Width > 0 ? requestedBounds.Width : 800;
        double height = requestedBounds.Height > 0 ? requestedBounds.Height : 600;

        if (displayAreas.Count == 0)
        {
            return CenterOnDisplay(width, height, defaultDisplay);
        }

        foreach (DisplayArea display in displayAreas)
        {
            double intersectLeft = Math.Max(requestedBounds.Left, display.Left);
            double intersectTop = Math.Max(requestedBounds.Top, display.Top);
            double intersectRight = Math.Min(requestedBounds.Right, display.Right);
            double intersectBottom = Math.Min(requestedBounds.Bottom, display.Bottom);

            double intersectWidth = Math.Max(0, intersectRight - intersectLeft);
            double intersectHeight = Math.Max(0, intersectBottom - intersectTop);

            if (intersectWidth >= MinVisibleDimension && intersectHeight >= MinVisibleDimension)
            {
                // The window is sufficiently visible on this active display.
                // Ensure the top edge is not hidden above the top of this monitor so the title bar can be grabbed.
                double safeTop = Math.Max(requestedBounds.Top, display.Top);
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
        double actualWidth = Math.Min(width, display.Width > 0 ? display.Width : width);
        double actualHeight = Math.Min(height, display.Height > 0 ? display.Height : height);

        double left = display.Left + Math.Max(0, (display.Width - actualWidth) / 2);
        double top = display.Top + Math.Max(0, (display.Height - actualHeight) / 2);

        return new WindowBounds(left, top, actualWidth, actualHeight);
    }
}
