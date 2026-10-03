using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Authentication-aware calendar provider boundary. Implementations own provider-specific
/// transport, event mapping, and cursor semantics; credentials are never passed through this contract.
/// </summary>
public interface ICalendarProvider
{
    CalendarProvider Provider { get; }

    bool SupportsIncrementalChanges { get; }
    bool SupportsEventWrites { get; }

    Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(
        Guid? accountId,
        string externalCalendarId,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken = default);

    Task<CalendarProviderChangePage> GetChangesAsync(
        Guid? accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken = default);

    Task<CalendarProviderEventIdentity> CreateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default);

    Task<CalendarProviderEventIdentity> UpdateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default);

    Task DeleteEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CancellationToken cancellationToken = default);
}
