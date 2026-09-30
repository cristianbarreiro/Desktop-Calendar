namespace CalendarWidget.Presentation.Models;

/// <summary>
/// Represents a single day cell in a 42-cell calendar grid.
/// </summary>
/// <param name="Date">The calendar date.</param>
/// <param name="DayNumber">The numerical day of the month.</param>
/// <param name="IsCurrentMonth">Whether the date belongs to the currently displayed month.</param>
/// <param name="IsToday">Whether the date corresponds to the current system day.</param>
/// <param name="HasEvents">Whether the date has any scheduled events.</param>
public sealed record CalendarDayModel(
    DateOnly Date,
    int DayNumber,
    bool IsCurrentMonth,
    bool IsToday,
    bool HasEvents = false)
{
    /// <summary>
    /// Gets a DateTime representation of the date for XAML bindings that require DateTime.
    /// </summary>
    public DateTime DateTime => Date.ToDateTime(TimeOnly.MinValue);

    /// <summary>
    /// Gets a full descriptive accessibility label conveying date, today status, and event presence.
    /// </summary>
    public string AccessibleDescription
    {
        get
        {
            string dateStr = Date.ToString("MMMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture);
            if (IsToday && HasEvents)
            {
                return $"{dateStr}, Today, has events";
            }

            if (IsToday)
            {
                return $"{dateStr}, Today";
            }

            if (HasEvents)
            {
                return $"{dateStr}, has events";
            }

            return dateStr;
        }
    }
}
