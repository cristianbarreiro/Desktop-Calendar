using CalendarWidget.Core.Enums;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Service managing runtime visual theme switching and resource dictionary synchronization.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current active theme mode.
    /// </summary>
    AppThemeMode ActiveTheme { get; }

    /// <summary>
    /// Applies the specified visual theme dynamically to the application.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    void ApplyTheme(AppThemeMode theme);

    /// <summary>
    /// Occurs when the active theme mode changes.
    /// </summary>
    event EventHandler<AppThemeMode>? ThemeChanged;
}
