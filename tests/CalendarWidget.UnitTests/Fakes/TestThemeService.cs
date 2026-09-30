using CalendarWidget.Core.Enums;
using CalendarWidget.Presentation.Services;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IThemeService"/> for unit tests.
/// </summary>
public sealed class TestThemeService : IThemeService
{
    public AppThemeMode ActiveTheme { get; private set; } = AppThemeMode.System;
    public int ApplyCount { get; private set; }

    public event EventHandler<AppThemeMode>? ThemeChanged;

    public void ApplyTheme(AppThemeMode theme)
    {
        ActiveTheme = theme;
        ApplyCount++;
        ThemeChanged?.Invoke(this, theme);
    }
}
