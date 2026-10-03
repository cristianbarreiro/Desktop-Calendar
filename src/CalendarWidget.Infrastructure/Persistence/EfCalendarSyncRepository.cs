using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.Infrastructure.Persistence;

public sealed class EfCalendarSyncRepository(AppDbContext context) : ICalendarSyncRepository
{
    public async Task<IReadOnlyList<PendingCalendarOperation>> GetPendingOperationsAsync(
        Guid calendarId, CancellationToken cancellationToken = default) =>
        await context.PendingCalendarOperations.AsNoTracking().Where(operation => operation.CalendarId == calendarId)
            .OrderBy(operation => operation.CreatedAt).ToListAsync(cancellationToken);

    public async Task SavePendingOperationAsync(PendingCalendarOperation operation, CancellationToken cancellationToken = default)
    {
        PendingCalendarOperation? existing = await context.PendingCalendarOperations.FirstOrDefaultAsync(item =>
            item.CalendarId == operation.CalendarId && item.InternalEventId == operation.InternalEventId && item.Type == operation.Type,
            cancellationToken);
        if (existing is null)
            context.PendingCalendarOperations.Add(operation);
        else
        {
            existing.LastError = operation.LastError;
            existing.CreatedAt = operation.CreatedAt;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePendingOperationAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        PendingCalendarOperation? operation = await context.PendingCalendarOperations.FindAsync([operationId], cancellationToken);
        if (operation is null)
            return;
        context.PendingCalendarOperations.Remove(operation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingCalendarOperation>> GetPendingOperationsForEventAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await context.PendingCalendarOperations.AsNoTracking().Where(operation => operation.InternalEventId == eventId)
            .ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<CalendarEventMapping>> GetMappingsAsync(
        Guid internalEventId,
        CancellationToken cancellationToken = default) =>
        await context.CalendarEventMappings.AsNoTracking()
            .Where(mapping => mapping.InternalEventId == internalEventId)
            .ToListAsync(cancellationToken);

    public async Task<CalendarSyncState?> GetStateAsync(Guid calendarId, CancellationToken cancellationToken = default) =>
        await context.CalendarSyncStates.AsNoTracking()
            .FirstOrDefaultAsync(state => state.CalendarId == calendarId, cancellationToken);

    public async Task<CalendarEventMapping?> FindMappingAsync(
        CalendarProvider provider,
        Guid accountId,
        Guid calendarId,
        string externalEventId,
        CancellationToken cancellationToken = default) =>
        await context.CalendarEventMappings.AsNoTracking().FirstOrDefaultAsync(mapping =>
            mapping.Provider == provider && mapping.AccountId == accountId &&
            mapping.CalendarId == calendarId && mapping.ExternalEventId == externalEventId,
            cancellationToken);

    public async Task<IReadOnlyList<CalendarEventMapping>> GetMappingsForCalendarAsync(
        CalendarProvider provider,
        Guid accountId,
        Guid calendarId,
        CancellationToken cancellationToken = default) =>
        await context.CalendarEventMappings.AsNoTracking().Where(mapping =>
                mapping.Provider == provider && mapping.AccountId == accountId && mapping.CalendarId == calendarId)
            .ToListAsync(cancellationToken);

    public async Task SaveMappingAsync(CalendarEventMapping mapping, CancellationToken cancellationToken = default)
    {
        mapping.Validate();
        CalendarAccount? account = await context.CalendarAccounts.AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == mapping.AccountId, cancellationToken);
        Calendar? calendar = await context.Calendars.AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == mapping.CalendarId, cancellationToken);
        if (account is null || calendar is null || account.Provider != mapping.Provider ||
            calendar.Provider != mapping.Provider || calendar.AccountId != mapping.AccountId)
        {
            throw new DomainValidationException("Event mapping provider, account, and calendar must match.");
        }

        CalendarEventMapping? current = await context.CalendarEventMappings.FirstOrDefaultAsync(existing =>
            existing.InternalEventId == mapping.InternalEventId && existing.Provider == mapping.Provider &&
            existing.AccountId == mapping.AccountId && existing.CalendarId == mapping.CalendarId,
            cancellationToken);
        if (current is null)
            context.CalendarEventMappings.Add(mapping);
        else
            context.Entry(current).CurrentValues.SetValues(mapping);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveImportedEventAsync(
        CalendarEvent calendarEvent,
        CalendarEventMapping mapping,
        CancellationToken cancellationToken = default)
    {
        calendarEvent.Validate();
        mapping.Validate();
        if (mapping.InternalEventId != calendarEvent.Id)
            throw new DomainValidationException("Imported event and mapping identifiers must match.");

        CalendarAccount? account = await context.CalendarAccounts.AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == mapping.AccountId, cancellationToken);
        Calendar? calendar = await context.Calendars.AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == mapping.CalendarId, cancellationToken);
        if (account is null || calendar is null || account.Provider != mapping.Provider ||
            calendar.Provider != mapping.Provider || calendar.AccountId != mapping.AccountId)
        {
            throw new DomainValidationException("Event mapping provider, account, and calendar must match.");
        }

        context.CalendarEvents.Add(calendarEvent);
        context.CalendarEventMappings.Add(mapping);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMappingAsync(CalendarEventMapping mapping, CancellationToken cancellationToken = default)
    {
        CalendarEventMapping? current = await context.CalendarEventMappings.FirstOrDefaultAsync(existing =>
                existing.InternalEventId == mapping.InternalEventId && existing.Provider == mapping.Provider &&
                existing.AccountId == mapping.AccountId && existing.CalendarId == mapping.CalendarId,
            cancellationToken);
        if (current is null)
            return;

        context.CalendarEventMappings.Remove(current);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveStateAsync(CalendarSyncState state, CancellationToken cancellationToken = default)
    {
        state.Validate();
        CalendarSyncState? current = await context.CalendarSyncStates
            .FirstOrDefaultAsync(existing => existing.CalendarId == state.CalendarId, cancellationToken);
        if (current is null)
            context.CalendarSyncStates.Add(state);
        else
            context.Entry(current).CurrentValues.SetValues(state);
        await context.SaveChangesAsync(cancellationToken);
    }
}
