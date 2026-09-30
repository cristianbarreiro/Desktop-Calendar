namespace CalendarWidget.App.Services;

/// <summary>
/// Provides access to the visible working areas of connected display monitors.
/// </summary>
public interface IDisplayMonitorProvider
{
    /// <summary>
    /// Gets the working areas for all currently connected and active displays.
    /// </summary>
    IReadOnlyList<DisplayArea> GetDisplayAreas();

    /// <summary>
    /// Gets the working area for the primary display.
    /// </summary>
    DisplayArea GetPrimaryDisplayArea();
}
