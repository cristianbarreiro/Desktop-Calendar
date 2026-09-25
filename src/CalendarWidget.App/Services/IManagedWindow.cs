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
    /// Occurs when the window is closed.
    /// </summary>
    event EventHandler Closed;
}
