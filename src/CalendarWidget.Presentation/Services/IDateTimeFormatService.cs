using CalendarWidget.Core.Entities;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Service managing dynamic date and time formatting based on user preferences.
/// </summary>
public interface IDateTimeFormatService
{
    /// <summary>
    /// Formats the digital clock time with seconds.
    /// </summary>
    string FormatClockTime(DateTime time);

    /// <summary>
    /// Formats an event or schedule timestamp (hours and minutes).
    /// </summary>
    string FormatTime(DateTime time);

    /// <summary>
    /// Formats a date using the user's preferred short date format.
    /// </summary>
    string FormatDate(DateOnly targetDate);

    /// <summary>
    /// Formats a DateTime using the user's preferred short date format.
    /// </summary>
    string FormatDate(DateTime dateTime);

    /// <summary>
    /// Formats an event time range or all-day label.
    /// </summary>
    string FormatEventRange(DateTime startTime, DateTime endTime, bool isAllDay);

    /// <summary>
    /// Updates the active format parameters from the provided user settings.
    /// </summary>
    /// <param name="settings">The current user settings.</param>
    void UpdateSettings(UserSettings settings);

    /// <summary>
    /// Occurs when date or time format preferences change.
    /// </summary>
    event EventHandler? FormatChanged;
}
