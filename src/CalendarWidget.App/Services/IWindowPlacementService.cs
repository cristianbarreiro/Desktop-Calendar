namespace CalendarWidget.App.Services;

/// <summary>
/// Manages window position restoration, off-screen recovery, and debounced persistence of window bounds.
/// </summary>
public interface IWindowPlacementService : IDisposable
{
    /// <summary>
    /// Applies saved bounds or centered default to the main application window with off-screen recovery.
    /// </summary>
    /// <param name="window">The main application window.</param>
    void ApplyMainWindowBounds(IManagedWindow window);

    /// <summary>
    /// Applies saved bounds or centered default to the compact widget window with off-screen recovery.
    /// </summary>
    /// <param name="window">The widget window.</param>
    void ApplyWidgetWindowBounds(IManagedWindow window);

    /// <summary>
    /// Records an updated bounds change for the main application window and schedules a debounced persist.
    /// </summary>
    /// <param name="left">Window left coordinate.</param>
    /// <param name="top">Window top coordinate.</param>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    void OnMainWindowBoundsChanged(double left, double top, double width, double height);

    /// <summary>
    /// Records an updated position change for the widget window and schedules a debounced persist.
    /// </summary>
    /// <param name="left">Window left coordinate.</param>
    /// <param name="top">Window top coordinate.</param>
    void OnWidgetWindowBoundsChanged(double left, double top);

    /// <summary>
    /// Flushes any pending debounced bounds change immediately to disk.
    /// </summary>
    Task FlushPendingSaveAsync();
}
