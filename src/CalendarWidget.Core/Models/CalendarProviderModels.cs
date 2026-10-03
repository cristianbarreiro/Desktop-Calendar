using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;

namespace CalendarWidget.Core.Models;

public sealed record CalendarProviderCalendar(string ExternalCalendarId, string Name);

public sealed record CalendarProviderEvent(string ExternalEventId, string? ExternalVersion, CalendarEvent Event);

public enum CalendarProviderChangeType
{
    Upsert,
    Delete
}

public sealed record CalendarProviderChange(
    CalendarProviderChangeType Type,
    string ExternalEventId,
    string? ExternalVersion,
    CalendarEvent? Event);

public sealed record CalendarProviderChangePage(
    IReadOnlyList<CalendarProviderChange> Changes,
    string? Cursor,
    bool IsFullSnapshot);

public sealed record CalendarProviderEventIdentity(string ExternalEventId, string? ExternalVersion);

public enum CalendarSynchronizationOutcome
{
    Created,
    Updated,
    Deleted,
    Skipped,
    Failed,
    Conflict,
}

public sealed record CalendarSynchronizationItemResult(
    Guid? InternalEventId,
    string? ExternalEventId,
    CalendarSynchronizationOutcome Outcome,
    string? Error = null);

public sealed record CalendarSynchronizationFailure(Guid? InternalEventId, string? ExternalEventId, string Error);

public sealed record CalendarSynchronizationCalendarResult(
    Guid CalendarId,
    CalendarProvider Provider,
    Guid? AccountId,
    CalendarSynchronizationResult Result);

public sealed record CalendarSynchronizationBatchResult(
    IReadOnlyList<CalendarSynchronizationCalendarResult> Calendars)
{
    public int Created => Calendars.Sum(item => item.Result.Created);
    public int Imported => Calendars.Sum(item => item.Result.EventsImported);
    public int Exported => Calendars.Sum(item => item.Result.EventsExported);
    public int Updated => Calendars.Sum(item => item.Result.Updated);
    public int Deleted => Calendars.Sum(item => item.Result.Deleted);
    public int Skipped => Calendars.Sum(item => item.Result.Skipped);
    public int Failed => Calendars.Sum(item => item.Result.Failed);
    public int Conflicts => Calendars.Sum(item => item.Result.Conflicts);
    public bool Succeeded => Failed == 0;
}

public sealed record CalendarSynchronizationResult
{
    public int Created { get; init; }
    public int Updated { get; init; }
    public int Deleted { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public int Conflicts { get; init; }
    public bool HasUnresolvedConflicts { get; init; }
    public string? Cursor { get; init; }
    public IReadOnlyList<CalendarSynchronizationFailure> Failures { get; init; } = [];
    public IReadOnlyList<CalendarSynchronizationItemResult> Items { get; init; } = [];

    public int EventsImported => Created;
    public int EventsExported { get; init; }
    public int EventsUpdated => Updated;
    public int EventsDeleted => Deleted;
    public int ConflictsResolved => Conflicts;

    public CalendarSynchronizationResult(
        int eventsImported, int eventsExported, int eventsUpdated, int eventsDeleted, int conflictsResolved, string? cursor)
    {
        Created = eventsImported;
        EventsExported = eventsExported;
        Updated = eventsUpdated;
        Deleted = eventsDeleted;
        Conflicts = conflictsResolved;
        Cursor = cursor;
    }

    public CalendarSynchronizationResult(
        int created, int updated, int deleted, int skipped, int failed, int conflicts,
        string? cursor, IReadOnlyList<CalendarSynchronizationFailure> failures,
        IReadOnlyList<CalendarSynchronizationItemResult> items, int eventsExported = 0)
    {
        Created = created;
        Updated = updated;
        Deleted = deleted;
        Skipped = skipped;
        Failed = failed;
        Conflicts = conflicts;
        Cursor = cursor;
        Failures = failures;
        Items = items;
        EventsExported = eventsExported;
    }
}
