namespace CalendarWidget.App.Services;

/// <summary>
/// Represents the visible working area of a display monitor in virtual screen coordinates.
/// </summary>
/// <param name="Left">Left coordinate in virtual screen pixels.</param>
/// <param name="Top">Top coordinate in virtual screen pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct DisplayArea(double Left, double Top, double Width, double Height)
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
