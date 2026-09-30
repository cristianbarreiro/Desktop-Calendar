using System.Windows;

namespace CalendarWidget.App.Services;

/// <summary>
/// Abstraction representing a managed window for testable state and lifecycle orchestration.
/// </summary>
public interface IManagedWindow
{
    /// <summary>
    /// Gets a value indicating whether the window is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets or sets the window state (Normal, Minimized, Maximized).
    /// </summary>
    WindowState WindowState { get; set; }

    /// <summary>
    /// Gets or sets whether the window appears in topmost z-order.
    /// </summary>
    bool Topmost { get; set; }

    /// <summary>
    /// Gets or sets the window opacity (0.0 to 1.0).
    /// </summary>
    double Opacity { get; set; }

    /// <summary>
    /// Displays the window.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the window.
    /// </summary>
    void Hide();

    /// <summary>
    /// Activates the window.
    /// </summary>
    bool Activate();

    /// <summary>
    /// Gets or sets the position of the window's left edge in relation to the desktop.
    /// </summary>
    double Left { get; set; }

    /// <summary>
    /// Gets or sets the position of the window's top edge in relation to the desktop.
    /// </summary>
    double Top { get; set; }

    /// <summary>
    /// Gets or sets the width of the window.
    /// </summary>
    double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the window.
    /// </summary>
    double Height { get; set; }

    /// <summary>
    /// Gets or sets the window startup location behavior.
    /// </summary>
    WindowStartupLocation WindowStartupLocation { get; set; }

    /// <summary>
    /// Occurs when the window is closed.
    /// </summary>
    event EventHandler Closed;
}
