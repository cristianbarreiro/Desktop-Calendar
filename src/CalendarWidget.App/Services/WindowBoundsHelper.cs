namespace CalendarWidget.App.Services;

/// <summary>
/// Provides deterministic validation and off-screen recovery logic for window coordinates across multiple monitors.
/// </summary>
public static class WindowBoundsHelper
{
    private const double MinVisibleDimension = 50.0;
    private const double DefaultWidgetWidth = 288.0;
    private const double DefaultWidgetHeight = 240.0;

    /// <summary>
    /// Fits a window within the usable area of the display where it is currently located.
    /// </summary>
    /// <param name="requestedBounds">The requested or current widget bounds.</param>
    /// <param name="displayAreas">The usable areas of connected displays.</param>
    /// <param name="defaultDisplay">The fallback display when no usable display is available.</param>
    /// <returns>Bounds fully contained by one display working area.</returns>
    public static WindowBounds EnsureFullyVisible(
        WindowBounds requestedBounds,
        IReadOnlyList<DisplayArea> displayAreas,
        DisplayArea defaultDisplay)
    {
        double width = IsPositiveFinite(requestedBounds.Width) ? requestedBounds.Width : DefaultWidgetWidth;
        double height = IsPositiveFinite(requestedBounds.Height) ? requestedBounds.Height : DefaultWidgetHeight;
        DisplayArea[] validDisplays = displayAreas
            .Where(IsValidDisplay)
            .ToArray();

        if (!IsFinite(requestedBounds.Left) || !IsFinite(requestedBounds.Top) || validDisplays.Length == 0)
        {
            return ClampToDisplay(requestedBounds.Left, requestedBounds.Top, width, height, defaultDisplay);
        }

        WindowBounds selectionBounds = new(requestedBounds.Left, requestedBounds.Top, width, height);
        DisplayArea targetDisplay = FindBestDisplay(selectionBounds, validDisplays);
        return ClampToDisplay(requestedBounds.Left, requestedBounds.Top, width, height, targetDisplay);
    }

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

    private static DisplayArea FindBestDisplay(WindowBounds bounds, DisplayArea[] displays)
    {
        foreach (DisplayArea display in displays)
        {
            if (bounds.Left >= display.Left && bounds.Left < display.Right &&
                bounds.Top >= display.Top && bounds.Top < display.Bottom)
            {
                return display;
            }
        }

        DisplayArea bestDisplay = displays[0];
        double bestIntersectionArea = -1;
        double bestDistance = double.PositiveInfinity;

        foreach (DisplayArea display in displays)
        {
            double intersectionWidth = Math.Max(0, Math.Min(bounds.Right, display.Right) - Math.Max(bounds.Left, display.Left));
            double intersectionHeight = Math.Max(0, Math.Min(bounds.Bottom, display.Bottom) - Math.Max(bounds.Top, display.Top));
            double intersectionArea = intersectionWidth * intersectionHeight;
            double horizontalDistance = Math.Max(0, Math.Max(display.Left - bounds.Left, bounds.Left - display.Right));
            double verticalDistance = Math.Max(0, Math.Max(display.Top - bounds.Top, bounds.Top - display.Bottom));
            double distance = (horizontalDistance * horizontalDistance) + (verticalDistance * verticalDistance);

            if (intersectionArea > bestIntersectionArea ||
                (intersectionArea == bestIntersectionArea && distance < bestDistance))
            {
                bestDisplay = display;
                bestIntersectionArea = intersectionArea;
                bestDistance = distance;
            }
        }

        return bestDisplay;
    }

    private static WindowBounds ClampToDisplay(double left, double top, double width, double height, DisplayArea display)
    {
        double safeWidth = IsPositiveFinite(width) ? width : DefaultWidgetWidth;
        double safeHeight = IsPositiveFinite(height) ? height : DefaultWidgetHeight;
        double displayLeft = IsFinite(display.Left) ? display.Left : 0;
        double displayTop = IsFinite(display.Top) ? display.Top : 0;
        double displayWidth = IsPositiveFinite(display.Width) ? display.Width : safeWidth;
        double displayHeight = IsPositiveFinite(display.Height) ? display.Height : safeHeight;
        double fittedWidth = Math.Min(safeWidth, displayWidth);
        double fittedHeight = Math.Min(safeHeight, displayHeight);
        double requestedLeft = IsFinite(left) ? left : displayLeft;
        double requestedTop = IsFinite(top) ? top : displayTop;
        double clampedLeft = Math.Clamp(requestedLeft, displayLeft, displayLeft + displayWidth - fittedWidth);
        double clampedTop = Math.Clamp(requestedTop, displayTop, displayTop + displayHeight - fittedHeight);

        return new WindowBounds(clampedLeft, clampedTop, fittedWidth, fittedHeight);
    }

    private static bool IsValidDisplay(DisplayArea display) =>
        IsFinite(display.Left) && IsFinite(display.Top) &&
        IsPositiveFinite(display.Width) && IsPositiveFinite(display.Height);

    private static bool IsPositiveFinite(double value) => value > 0 && IsFinite(value);

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
