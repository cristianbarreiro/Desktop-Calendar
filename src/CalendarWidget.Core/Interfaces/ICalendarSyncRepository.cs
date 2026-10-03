using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Persistence operations for external event mappings and opaque sync checkpoints.
/// </summary>
public interface ICalendarSyncRepository
{
    Task<IReadOnlyList<CalendarEventMapping>> GetMappingsAsync(Guid internalEventId, CancellationToken cancellationToken = default);
    Task<CalendarEventMapping?> FindMappingAsync(
        CalendarProvider provider,
        Guid accountId,
        Guid calendarId,
        string externalEventId,
        CancellationToken cancellationToken = default);
    Task<CalendarSyncState?> GetStateAsync(Guid calendarId, CancellationToken cancellationToken = default);
    Task SaveMappingAsync(CalendarEventMapping mapping, CancellationToken cancellationToken = default);
    Task SaveImportedEventAsync(CalendarEvent calendarEvent, CalendarEventMapping mapping, CancellationToken cancellationToken = default);
    Task DeleteMappingAsync(CalendarEventMapping mapping, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarEventMapping>> GetMappingsForCalendarAsync(
        CalendarProvider provider,
        Guid accountId,
        Guid calendarId,
        CancellationToken cancellationToken = default);
    Task SaveStateAsync(CalendarSyncState state, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingCalendarOperation>> GetPendingOperationsAsync(Guid calendarId, CancellationToken cancellationToken = default);
    Task SavePendingOperationAsync(PendingCalendarOperation operation, CancellationToken cancellationToken = default);
    Task DeletePendingOperationAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingCalendarOperation>> GetPendingOperationsForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
