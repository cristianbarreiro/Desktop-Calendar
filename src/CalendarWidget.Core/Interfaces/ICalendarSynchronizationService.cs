using CalendarWidget.Core.Models;

namespace CalendarWidget.Core.Interfaces;

public interface ICalendarSynchronizationService
{
    Task<CalendarSynchronizationResult> SynchronizeCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default);

    Task<CalendarSynchronizationBatchResult> SynchronizeEnabledCalendarsAsync(
        CancellationToken cancellationToken = default);

    Task<CalendarSynchronizationResult> DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
