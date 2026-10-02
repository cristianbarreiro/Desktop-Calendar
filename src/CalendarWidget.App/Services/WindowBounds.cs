namespace CalendarWidget.App.Services;

/// <summary>
/// Represents the rectangle bounds of a WPF window in device-independent units.
/// </summary>
/// <param name="Left">Left coordinate in WPF device-independent units.</param>
/// <param name="Top">Top coordinate in WPF device-independent units.</param>
/// <param name="Width">Width in device-independent units.</param>
/// <param name="Height">Height in device-independent units.</param>
public readonly record struct WindowBounds(double Left, double Top, double Width, double Height)
{
    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public double Right => Left + Width;

    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public double Bottom => Top + Height;
}
