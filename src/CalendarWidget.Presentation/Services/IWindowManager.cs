namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Defines window orchestration operations for switching between the compact desktop widget
/// and the full application window.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Displays the main full-application window and hides or minimizes the widget.
    /// </summary>
    void ShowFullApplication();

    /// <summary>
    /// Displays the compact desktop widget and hides the main full-application window.
    /// </summary>
    void ShowWidget();

    /// <summary>
    /// Minimizes the active widget window.
    /// </summary>
    void MinimizeWidget();

    /// <summary>
    /// Gets a value indicating whether the full application window is currently visible.
    /// </summary>
    bool IsFullApplicationVisible { get; }

    /// <summary>
    /// Gets a value indicating whether the widget window is currently visible.
    /// </summary>
    bool IsWidgetVisible { get; }
}
