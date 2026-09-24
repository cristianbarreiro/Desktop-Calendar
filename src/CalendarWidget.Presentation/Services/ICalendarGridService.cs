using CalendarWidget.Presentation.Models;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Service responsible for generating a deterministic 42-cell calendar grid for a given month and year.
/// </summary>
public interface ICalendarGridService
{
    /// <summary>
    /// Generates a 42-cell (7 columns × 6 rows) calendar grid for the specified year and month.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="firstDayOfWeek">The configured first day of the week.</param>
    /// <param name="today">The date considered to be "today".</param>
    /// <returns>A read-only list containing exactly 42 calendar day models.</returns>
    IReadOnlyList<CalendarDayModel> GenerateGrid(int year, int month, DayOfWeek firstDayOfWeek, DateOnly today);

    /// <summary>
    /// Generates day-of-week abbreviations ordered starting from the configured first day of the week.
    /// </summary>
    /// <param name="firstDayOfWeek">The configured first day of the week.</param>
    /// <returns>A read-only list of 7 weekday abbreviation strings.</returns>
    IReadOnlyList<string> GetDayHeaders(DayOfWeek firstDayOfWeek);
}
