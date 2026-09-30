namespace CalendarWidget.App.Services;

/// <summary>
/// Manages the Windows notification area (System Tray) icon, context menu, and shell interactions.
/// </summary>
public interface ITrayService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the tray icon is currently active and visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Initializes and displays the system tray icon with context actions.
    /// </summary>
    void Initialize();
}
