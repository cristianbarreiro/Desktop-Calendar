namespace CalendarWidget.App.Services;

/// <summary>
/// Represents a display working area in WPF device-independent units.
/// </summary>
/// <param name="Left">Left coordinate in WPF device-independent units.</param>
/// <param name="Top">Top coordinate in WPF device-independent units.</param>
/// <param name="Width">Width in WPF device-independent units.</param>
/// <param name="Height">Height in WPF device-independent units.</param>
public readonly record struct DisplayArea(double Left, double Top, double Width, double Height)
{
    /// <summary>
    /// Converts a device-pixel working area to WPF device-independent units.
    /// </summary>
    public static DisplayArea FromDevicePixels(double left, double top, double width, double height, double dpiX, double dpiY)
    {
        double scaleX = dpiX > 0 ? dpiX / 96.0 : 1.0;
        double scaleY = dpiY > 0 ? dpiY / 96.0 : 1.0;
        return new DisplayArea(left / scaleX, top / scaleY, width / scaleX, height / scaleY);
    }

    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public double Right => Left + Width;

    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public double Bottom => Top + Height;
}
