using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents persisted user preferences and application configuration settings.
/// </summary>
public sealed class UserSettings
{
    /// <summary>
    /// Gets or sets the visual theme preference.
    /// </summary>
    public AppThemeMode Theme { get; set; } = AppThemeMode.Dark;

    /// <summary>
    /// Gets or sets whether the desktop calendar widget is pinned always on top of other windows.
    /// </summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets whether the application launches automatically on Windows logon.
    /// </summary>
    public bool StartWithWindows { get; set; }

    /// <summary>
    /// Gets or sets the opacity of the desktop widget window, from 0.5 (50%) to 1.0 (100%).
    /// </summary>
    public double WidgetOpacity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the first day of the week used for calendar grids.
    /// </summary>
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Gets or sets the time display formatting preference (24-hour or 12-hour).
    /// </summary>
    public TimeFormatOption TimeFormat { get; set; } = TimeFormatOption.TwentyFourHour;

    /// <summary>
    /// Gets or sets the short date formatting preference.
    /// </summary>
    public string DateFormat { get; set; } = "Default";

    /// <summary>
    /// Validates the domain invariants for user settings.
    /// </summary>
    /// <exception cref="DomainValidationException">Thrown when settings values violate domain rules.</exception>
    public void Validate()
    {
        if (WidgetOpacity is < 0.5 or > 1.0)
        {
            throw new DomainValidationException(
                $"Widget opacity must be between 0.5 (50%) and 1.0 (100%). Value was: {WidgetOpacity}.");
        }

        if (string.IsNullOrWhiteSpace(DateFormat))
        {
            throw new DomainValidationException("Date format identifier cannot be empty or whitespace.");
        }

        if (!Enum.IsDefined(Theme))
        {
            throw new DomainValidationException($"Invalid theme mode: {Theme}.");
        }

        if (!Enum.IsDefined(TimeFormat))
        {
            throw new DomainValidationException($"Invalid time format option: {TimeFormat}.");
        }

        if (!Enum.IsDefined(FirstDayOfWeek))
        {
            throw new DomainValidationException($"Invalid first day of week: {FirstDayOfWeek}.");
        }
    }

    /// <summary>
    /// Creates a memberwise copy of the current settings.
    /// </summary>
    public UserSettings Clone() => new()
    {
        Theme = Theme,
        AlwaysOnTop = AlwaysOnTop,
        StartWithWindows = StartWithWindows,
        WidgetOpacity = WidgetOpacity,
        FirstDayOfWeek = FirstDayOfWeek,
        TimeFormat = TimeFormat,
        DateFormat = DateFormat,
    };
}
