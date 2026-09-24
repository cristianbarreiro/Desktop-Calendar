using CalendarWidget.Presentation.Models;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Generates deterministic 42-cell calendar grids and day headers based on date and first-day-of-week settings.
/// </summary>
public sealed class CalendarGridService : ICalendarGridService
{
    private const int TotalGridCells = 42;

    /// <inheritdoc />
    public IReadOnlyList<CalendarDayModel> GenerateGrid(int year, int month, DayOfWeek firstDayOfWeek, DateOnly today)
    {
        DateOnly firstOfMonth = new(year, month, 1);
        int dayOfWeekOffset = ((int)firstOfMonth.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        DateOnly gridStartDate = firstOfMonth.AddDays(-dayOfWeekOffset);

        List<CalendarDayModel> cells = new(TotalGridCells);

        for (int i = 0; i < TotalGridCells; i++)
        {
            DateOnly currentDate = gridStartDate.AddDays(i);
            bool isCurrentMonth = currentDate.Year == year && currentDate.Month == month;
            bool isToday = currentDate == today;

            cells.Add(new CalendarDayModel(
                Date: currentDate,
                DayNumber: currentDate.Day,
                IsCurrentMonth: isCurrentMonth,
                IsToday: isToday));
        }

        return cells;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDayHeaders(DayOfWeek firstDayOfWeek)
    {
        string[] headers = new string[7];
        for (int i = 0; i < 7; i++)
        {
            DayOfWeek day = (DayOfWeek)(((int)firstDayOfWeek + i) % 7);
            headers[i] = day switch
            {
                DayOfWeek.Sunday => "Su",
                DayOfWeek.Monday => "Mo",
                DayOfWeek.Tuesday => "Tu",
                DayOfWeek.Wednesday => "We",
                DayOfWeek.Thursday => "Th",
                DayOfWeek.Friday => "Fr",
                DayOfWeek.Saturday => "Sa",
                _ => string.Empty
            };
        }

        return headers;
    }
}
