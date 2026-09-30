namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Abstraction for detecting the host operating system's dark/light theme setting.
/// </summary>
public interface ISystemThemeDetector
{
    /// <summary>
    /// Checks whether the operating system is currently using a dark theme.
    /// </summary>
    /// <returns>True if system is using a dark theme; otherwise false.</returns>
    bool IsSystemInDarkTheme();
}
