using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Repository for calendar event persistence operations.
/// </summary>
public interface ICalendarEventRepository
{
    /// <summary>Gets an event by its identifier.</summary>
    Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Gets events intersecting the specified date range.</summary>
    Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(DateTime start, DateTime endDate, CancellationToken cancellationToken = default);
    /// <summary>Adds a calendar event.</summary>
    Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    /// <summary>Updates a calendar event.</summary>
    Task UpdateAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    /// <summary>Deletes an event by identifier.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
