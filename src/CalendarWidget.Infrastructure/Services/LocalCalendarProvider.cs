using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>Adapts the existing local event repository to the provider contract.</summary>
public sealed class LocalCalendarProvider(ICalendarEventRepository events, ICalendarCatalogRepository calendars)
    : ICalendarProvider
{
    public CalendarProvider Provider => CalendarProvider.Local;
    public bool SupportsIncrementalChanges => false;
    public bool SupportsEventWrites => true;

    public async Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Calendar> localCalendars = await calendars.GetCalendarsAsync(cancellationToken);
        return localCalendars.Where(calendar => calendar.Provider == Provider && calendar.AccountId == accountId)
            .Select(calendar => new CalendarProviderCalendar(calendar.ExternalId, calendar.Name)).ToArray();
    }

    public async Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(
        Guid? accountId,
        string externalCalendarId,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rangeEndUtc, rangeStartUtc);

        Guid calendarId = await ResolveCalendarIdAsync(externalCalendarId, cancellationToken);
        IReadOnlyList<CalendarEvent> candidates = await events.GetByDateRangeAsync(
            rangeStartUtc, rangeEndUtc, cancellationToken);
        return candidates.Where(calendarEvent => calendarEvent.CalendarId == calendarId)
            .Select(ToProviderEvent).ToArray();
    }

    public async Task<CalendarProviderChangePage> GetChangesAsync(
        Guid? accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        Guid calendarId = await ResolveCalendarIdAsync(externalCalendarId, cancellationToken);
        IReadOnlyList<CalendarEvent> allEvents = await events.GetAllAsync(cancellationToken);
        CalendarProviderChange[] snapshot = allEvents.Where(calendarEvent => calendarEvent.CalendarId == calendarId)
            .Select(calendarEvent => new CalendarProviderChange(
                CalendarProviderChangeType.Upsert,
                calendarEvent.Id.ToString("D"),
                GetVersion(calendarEvent),
                calendarEvent))
            .ToArray();
        return new CalendarProviderChangePage(snapshot, null, IsFullSnapshot: true);
    }

    public async Task<CalendarProviderEventIdentity> CreateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        calendarEvent.Id = Guid.NewGuid();
        calendarEvent.CalendarId = await ResolveCalendarIdAsync(externalCalendarId, cancellationToken);
        calendarEvent.Validate();
        await events.AddAsync(calendarEvent, cancellationToken);
        return Identity(calendarEvent);
    }

    public async Task<CalendarProviderEventIdentity> UpdateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        CalendarEvent current = await FindEventAsync(externalCalendarId, externalEventId, cancellationToken);
        calendarEvent.Id = current.Id;
        calendarEvent.CalendarId = current.CalendarId;
        calendarEvent.CreatedAt = current.CreatedAt;
        if (calendarEvent.UpdatedAt <= current.UpdatedAt)
            calendarEvent.UpdatedAt = DateTime.UtcNow;
        calendarEvent.Validate();
        await events.UpdateAsync(calendarEvent, cancellationToken);
        return Identity(calendarEvent);
    }

    public async Task DeleteEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        CalendarEvent current = await FindEventAsync(externalCalendarId, externalEventId, cancellationToken);
        await events.DeleteAsync(current.Id, cancellationToken);
    }

    private async Task<CalendarEvent> FindEventAsync(
        string externalCalendarId,
        string externalEventId,
        CancellationToken cancellationToken)
    {
        Guid calendarId = await ResolveCalendarIdAsync(externalCalendarId, cancellationToken);
        if (!Guid.TryParse(externalEventId, out Guid eventId))
            throw new KeyNotFoundException("Local event identifier is invalid.");

        CalendarEvent? calendarEvent = await events.GetByIdAsync(eventId, cancellationToken);
        return calendarEvent is not null && calendarEvent.CalendarId == calendarId
            ? calendarEvent
            : throw new KeyNotFoundException("Local event was not found in the requested calendar.");
    }

    private static CalendarProviderEvent ToProviderEvent(CalendarEvent calendarEvent) =>
        new(calendarEvent.Id.ToString("D"), GetVersion(calendarEvent), calendarEvent);

    private static CalendarProviderEventIdentity Identity(CalendarEvent calendarEvent) =>
        new(calendarEvent.Id.ToString("D"), GetVersion(calendarEvent));

    private static string GetVersion(CalendarEvent calendarEvent) =>
        calendarEvent.UpdatedAt.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task<Guid> ResolveCalendarIdAsync(string externalCalendarId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Calendar> availableCalendars = await calendars.GetCalendarsAsync(cancellationToken);
        Calendar calendar = availableCalendars
            .SingleOrDefault(candidate => candidate.Provider == Provider && candidate.ExternalId == externalCalendarId)
            ?? throw new KeyNotFoundException("Local calendar was not found.");
        return calendar.Id;
    }
}
