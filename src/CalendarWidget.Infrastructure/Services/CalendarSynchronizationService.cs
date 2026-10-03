using System.Security.Cryptography;
using System.Text;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>Coordinates provider changes, local persistence, mappings and opaque checkpoints.</summary>
public sealed class CalendarSynchronizationService(
    ICalendarCatalogRepository catalogs,
    ICalendarEventRepository events,
    ICalendarSyncRepository sync,
    ICalendarProviderRegistry providers,
    ICalendarConflictPolicy? conflictPolicy = null,
    AppDbContext? dbContext = null)
{
    private readonly ICalendarConflictPolicy resolvedConflictPolicy =
        conflictPolicy ?? new LocalWinsCalendarConflictPolicy();

    public async Task<CalendarSynchronizationResult> DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        CalendarEvent? calendarEvent = await events.GetByIdAsync(eventId, cancellationToken);
        if (calendarEvent is null)
            return Result(skipped: 1);

        IReadOnlyList<CalendarEventMapping> mappings = await sync.GetMappingsAsync(eventId, cancellationToken);
        IReadOnlyList<Calendar> calendars = await catalogs.GetCalendarsAsync(cancellationToken);
        List<CalendarSynchronizationFailure> failures = [];
        foreach (CalendarEventMapping mapping in mappings)
        {
            Calendar? calendar = calendars.SingleOrDefault(item => item.Id == mapping.CalendarId);
            if (calendar is null || calendar.Provider != mapping.Provider || calendar.AccountId != mapping.AccountId ||
                mapping.InternalEventId != eventId ||
                (calendarEvent.CalendarId != CalendarIdentity.LocalCalendarId && calendarEvent.CalendarId != mapping.CalendarId))
            {
                failures.Add(new CalendarSynchronizationFailure(eventId, mapping.ExternalEventId,
                    "The event mapping does not match its provider account and calendar; local event was retained."));
                break;
            }

            ICalendarProvider provider = providers.GetProvider(mapping.Provider);
            if (!provider.SupportsEventWrites)
            {
                failures.Add(new CalendarSynchronizationFailure(eventId, mapping.ExternalEventId,
                    "The mapped provider does not support deletion; local event was retained."));
                break;
            }

            try
            {
                await sync.SavePendingOperationAsync(new PendingCalendarOperation
                {
                    Id = Guid.NewGuid(),
                    CalendarId = mapping.CalendarId,
                    InternalEventId = eventId,
                    Type = PendingCalendarOperationType.Delete,
                    CreatedAt = DateTime.UtcNow,
                }, cancellationToken);
                await provider.DeleteEventAsync(mapping.AccountId, calendar.ExternalId, mapping.ExternalEventId,
                    mapping.ExternalVersion, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await sync.SavePendingOperationAsync(new PendingCalendarOperation
                {
                    Id = Guid.NewGuid(),
                    CalendarId = mapping.CalendarId,
                    InternalEventId = eventId,
                    Type = PendingCalendarOperationType.Delete,
                    CreatedAt = DateTime.UtcNow,
                    LastError = ex.Message,
                }, cancellationToken);
                failures.Add(new CalendarSynchronizationFailure(eventId, mapping.ExternalEventId,
                    $"External deletion failed; local event was retained: {ex.Message}"));
                break;
            }
        }

        if (failures.Count != 0)
            return new CalendarSynchronizationResult(0, 0, 0, 0, failures.Count, 0, null, failures,
                failures.Select(failure => new CalendarSynchronizationItemResult(
                    failure.InternalEventId, failure.ExternalEventId, CalendarSynchronizationOutcome.Failed, failure.Error)).ToArray());

        await events.DeleteAsync(eventId, cancellationToken);
        foreach (PendingCalendarOperation operation in await sync.GetPendingOperationsForEventAsync(eventId, cancellationToken))
            await sync.DeletePendingOperationAsync(operation.Id, cancellationToken);
        return Result(deleted: 1);
    }

    public async Task<CalendarSynchronizationResult> SynchronizeCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = dbContext is null
            ? null
            : await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            CalendarSynchronizationResult result = await SynchronizeCalendarCoreAsync(calendarId, cancellationToken);
            if (result.Failed > 0 || result.HasUnresolvedConflicts)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    dbContext!.ChangeTracker.Clear();
                }
                await RecordFailedSynchronizationAsync(calendarId, result, cancellationToken);
                return result;
            }
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DomainValidationException and not KeyNotFoundException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext!.ChangeTracker.Clear();
            }
            CalendarSynchronizationFailure failure = new(null, null, ex.Message);
            CalendarSynchronizationResult result = new(0, 0, 0, 0, 1, 0, null,
                [failure], [new CalendarSynchronizationItemResult(null, null, CalendarSynchronizationOutcome.Failed, failure.Error)]);
            await RecordFailedSynchronizationAsync(calendarId, result, cancellationToken);
            return result;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task RecordFailedSynchronizationAsync(
        Guid calendarId, CalendarSynchronizationResult result, CancellationToken cancellationToken)
    {
        CalendarSyncState? previousState = await sync.GetStateAsync(calendarId, cancellationToken);
        string error = result.Failures.Count > 0
            ? result.Failures[0].Error
            : "Calendar synchronization requires conflict resolution.";
        await sync.SaveStateAsync(new CalendarSyncState
        {
            CalendarId = calendarId,
            Cursor = previousState?.Cursor,
            LastSyncedAt = previousState?.LastSyncedAt,
            LastError = error,
        }, cancellationToken);
        foreach (Guid eventId in result.Failures.Where(item => item.InternalEventId.HasValue)
                     .Select(item => item.InternalEventId!.Value).Distinct())
        {
            await sync.SavePendingOperationAsync(new PendingCalendarOperation
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId,
                InternalEventId = eventId,
                Type = PendingCalendarOperationType.Upsert,
                CreatedAt = DateTime.UtcNow,
                LastError = error,
            }, cancellationToken);
        }
    }

    private async Task<CalendarSynchronizationResult> SynchronizeCalendarCoreAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        Calendar? calendar = (await catalogs.GetCalendarsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == calendarId);
        if (calendar is null)
            throw new KeyNotFoundException("Calendar was not found.");
        if (calendar.Provider == CalendarProvider.Local || calendar.AccountId is not Guid accountId)
            throw new DomainValidationException("Only connected provider calendars can be synchronized.");
        if (!calendar.IsEnabled)
            throw new DomainValidationException("Calendar synchronization is disabled for this calendar.");
        CalendarAccount? account = (await catalogs.GetAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId && candidate.Provider == calendar.Provider);
        if (account is null || !account.IsConnected)
            throw new InvalidOperationException("Calendar provider account is disconnected.");

        ICalendarProvider provider = providers.GetProvider(calendar.Provider);
        CalendarSyncState? currentState = await sync.GetStateAsync(calendar.Id, cancellationToken);
        IReadOnlyList<PendingCalendarOperation> pendingOperations =
            await sync.GetPendingOperationsAsync(calendar.Id, cancellationToken);
        int pendingDeleted = 0;
        List<CalendarSynchronizationItemResult> pendingDeleteItems = [];
        foreach (PendingCalendarOperation operation in pendingOperations.Where(item => item.Type == PendingCalendarOperationType.Delete))
        {
            try
            {
                CalendarEventMapping? pendingMapping = (await sync.GetMappingsAsync(operation.InternalEventId, cancellationToken))
                    .SingleOrDefault(item => item.CalendarId == calendar.Id);
                if (pendingMapping is not null)
                    await provider.DeleteEventAsync(accountId, calendar.ExternalId, pendingMapping.ExternalEventId,
                        pendingMapping.ExternalVersion, cancellationToken);
                if (pendingMapping is not null)
                    await sync.DeleteMappingAsync(pendingMapping, cancellationToken);
                await sync.DeletePendingOperationAsync(operation.Id, cancellationToken);
                pendingDeleted++;
                pendingDeleteItems.Add(new CalendarSynchronizationItemResult(
                    operation.InternalEventId, pendingMapping?.ExternalEventId, CalendarSynchronizationOutcome.Deleted));
                IReadOnlyList<CalendarEventMapping> remainingMappings = await sync.GetMappingsAsync(operation.InternalEventId, cancellationToken);
                IReadOnlyList<PendingCalendarOperation> remainingOperations =
                    await sync.GetPendingOperationsForEventAsync(operation.InternalEventId, cancellationToken);
                if (remainingMappings.Count == 0 && remainingOperations.Count == 0)
                    await events.DeleteAsync(operation.InternalEventId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                operation.LastError = ex.Message;
                await sync.SavePendingOperationAsync(operation, cancellationToken);
                return new CalendarSynchronizationResult(0, 0, 0, 0, 1, 0, currentState?.Cursor,
                    [new CalendarSynchronizationFailure(operation.InternalEventId, null, ex.Message)],
                    [new CalendarSynchronizationItemResult(operation.InternalEventId, null, CalendarSynchronizationOutcome.Failed, ex.Message)]);
            }
        }
        CalendarProviderChangePage page;
        try
        {
            page = await provider.GetChangesAsync(accountId, calendar.ExternalId, currentState?.Cursor, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await sync.SaveStateAsync(new CalendarSyncState
            {
                CalendarId = calendar.Id,
                Cursor = currentState?.Cursor,
                LastSyncedAt = currentState?.LastSyncedAt,
                LastError = ex.Message,
            }, cancellationToken);
            throw;
        }
        IReadOnlyList<CalendarEventMapping> mappings = await sync.GetMappingsForCalendarAsync(
            calendar.Provider, accountId, calendar.Id, cancellationToken);
        Dictionary<string, CalendarEventMapping> mappingsByExternalId = mappings
            .ToDictionary(mapping => mapping.ExternalEventId, StringComparer.Ordinal);
        HashSet<Guid> mappedInternalIds = mappings.Select(mapping => mapping.InternalEventId).ToHashSet();

        int imported = 0;
        int exported = 0;
        int updated = 0;
        int deleted = pendingDeleted;
        int conflicts = 0;
        int skipped = 0;
        int failed = 0;
        List<CalendarSynchronizationFailure> failures = [];
        List<CalendarSynchronizationItemResult> items = pendingDeleteItems;
        HashSet<string> observedExternalIds = new(StringComparer.Ordinal);
        HashSet<Guid> unresolvedConflictEventIds = [];
        DateTime now = DateTime.UtcNow;
        bool requiresManualResolution = false;

        foreach (CalendarProviderChange change in page.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            observedExternalIds.Add(change.ExternalEventId);
            mappingsByExternalId.TryGetValue(change.ExternalEventId, out CalendarEventMapping? mapping);

            if (change.Type == CalendarProviderChangeType.Delete)
            {
                if (mapping is null)
                {
                    skipped++;
                    items.Add(new CalendarSynchronizationItemResult(null, change.ExternalEventId, CalendarSynchronizationOutcome.Skipped));
                    continue;
                }
                CalendarEvent? mappedEvent = await events.GetByIdAsync(mapping.InternalEventId, cancellationToken);
                if (provider.SupportsEventWrites && mappedEvent is not null && IsLocallyChanged(mappedEvent, mapping))
                {
                    conflicts++;
                    CalendarConflictResolution resolution = resolvedConflictPolicy.Resolve(mappedEvent, change);
                    if (resolution == CalendarConflictResolution.PreferLocal)
                    {
                        CalendarProviderEventIdentity? identity = await TryProviderWriteAsync(
                            () => provider.CreateEventAsync(accountId, calendar.ExternalId, CopyEvent(mappedEvent), cancellationToken),
                            mappedEvent.Id, change.ExternalEventId);
                        if (identity is null)
                            continue;
                        mapping.ExternalEventId = identity.ExternalEventId;
                        mapping.ExternalVersion = identity.ExternalVersion;
                        mapping.LastSyncedAt = now;
                        mapping.LastSyncedLocalVersion = GetLocalVersion(mappedEvent);
                        await sync.SaveMappingAsync(mapping, cancellationToken);
                        exported++;
                    }
                    else if (resolution == CalendarConflictResolution.PreferRemote)
                    {
                        await events.DeleteAsync(mappedEvent.Id, cancellationToken);
                        await sync.DeleteMappingAsync(mapping, cancellationToken);
                        deleted++;
                    }
                    else
                    {
                        requiresManualResolution = true;
                        unresolvedConflictEventIds.Add(mappedEvent.Id);
                    }
                    items.Add(new CalendarSynchronizationItemResult(
                        mappedEvent.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Conflict));
                }
                else
                {
                    if (mappedEvent is not null)
                        await events.DeleteAsync(mappedEvent.Id, cancellationToken);
                    await sync.DeleteMappingAsync(mapping, cancellationToken);
                    deleted++;
                    items.Add(new CalendarSynchronizationItemResult(mapping.InternalEventId, change.ExternalEventId, CalendarSynchronizationOutcome.Deleted));
                }
                continue;
            }

            if (change.Event is null)
                throw new DomainValidationException("Provider upsert changes must include an event payload.");

            if (mapping is null)
            {
                CalendarEvent localEvent = CopyEvent(change.Event);
                localEvent.Id = Guid.NewGuid();
                localEvent.CalendarId = calendar.Id;
                localEvent.CreatedAt = change.Event.CreatedAt;
                localEvent.UpdatedAt = now;
                mapping = NewMapping(localEvent.Id, calendar, accountId, change, now);
                await sync.SaveImportedEventAsync(localEvent, mapping, cancellationToken);
                mappingsByExternalId.Add(change.ExternalEventId, mapping);
                mappedInternalIds.Add(localEvent.Id);
                imported++;
                items.Add(new CalendarSynchronizationItemResult(localEvent.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Created));
                continue;
            }

            CalendarEvent? existing = await events.GetByIdAsync(mapping.InternalEventId, cancellationToken);
            string? resultingVersion = change.ExternalVersion;
            bool localChangesPending = existing is not null && IsLocallyChanged(existing, mapping);
            bool providerWriteCompleted = false;
            if (existing is null)
            {
                CalendarEvent restored = CopyEvent(change.Event);
                restored.Id = mapping.InternalEventId;
                restored.CalendarId = calendar.Id;
                restored.UpdatedAt = now;
                await events.AddAsync(restored, cancellationToken);
                mapping.LastSyncedLocalVersion = GetLocalVersion(restored);
                updated++;
                items.Add(new CalendarSynchronizationItemResult(restored.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Updated));
            }
            else if (provider.SupportsEventWrites && localChangesPending && mapping.ExternalVersion != change.ExternalVersion)
            {
                conflicts++;
                CalendarConflictResolution resolution = resolvedConflictPolicy.Resolve(existing, change);
                if (resolution == CalendarConflictResolution.PreferLocal)
                {
                    CalendarProviderEventIdentity? identity = await TryProviderWriteAsync(
                        () => provider.UpdateEventAsync(accountId, calendar.ExternalId, mapping.ExternalEventId,
                            mapping.ExternalVersion, CopyEvent(existing), cancellationToken),
                        existing.Id, mapping.ExternalEventId);
                    if (identity is null)
                        continue;
                    mapping.ExternalVersion = identity.ExternalVersion;
                    mapping.LastSyncedAt = now;
                    resultingVersion = identity.ExternalVersion;
                    providerWriteCompleted = true;
                    exported++;
                }
                else if (resolution == CalendarConflictResolution.PreferRemote)
                {
                    ApplyProviderFields(existing, change.Event);
                    existing.UpdatedAt = now;
                    await events.UpdateAsync(existing, cancellationToken);
                    mapping.LastSyncedAt = now;
                    updated++;
                }
                else
                {
                    requiresManualResolution = true;
                    unresolvedConflictEventIds.Add(existing.Id);
                    items.Add(new CalendarSynchronizationItemResult(
                        existing.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Conflict));
                    continue;
                }
                items.Add(new CalendarSynchronizationItemResult(existing.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Conflict));
            }
            else if (mapping.ExternalVersion != change.ExternalVersion)
            {
                ApplyProviderFields(existing, change.Event);
                existing.UpdatedAt = now;
                await events.UpdateAsync(existing, cancellationToken);
                updated++;
                items.Add(new CalendarSynchronizationItemResult(existing.Id, change.ExternalEventId, CalendarSynchronizationOutcome.Updated));
            }
            else
            {
                skipped++;
                items.Add(new CalendarSynchronizationItemResult(existing?.Id ?? mapping.InternalEventId,
                    change.ExternalEventId, CalendarSynchronizationOutcome.Skipped));
            }

            mapping.ExternalVersion = resultingVersion;
            if (!localChangesPending || providerWriteCompleted)
            {
                mapping.LastSyncedAt = now;
                CalendarEvent? synchronized = await events.GetByIdAsync(mapping.InternalEventId, cancellationToken);
                if (synchronized is not null)
                    mapping.LastSyncedLocalVersion = GetLocalVersion(synchronized);
            }
            await sync.SaveMappingAsync(mapping, cancellationToken);
        }

        if (page.IsFullSnapshot)
        {
            foreach (CalendarEventMapping mapping in mappings)
            {
                if (observedExternalIds.Contains(mapping.ExternalEventId))
                    continue;
                CalendarEvent? localEvent = await events.GetByIdAsync(mapping.InternalEventId, cancellationToken);
                if (provider.SupportsEventWrites && localEvent is not null && IsLocallyChanged(localEvent, mapping))
                {
                    conflicts++;
                    CalendarConflictResolution resolution = resolvedConflictPolicy.Resolve(
                        localEvent,
                        new CalendarProviderChange(CalendarProviderChangeType.Delete, mapping.ExternalEventId,
                            mapping.ExternalVersion, null));
                    if (resolution == CalendarConflictResolution.PreferLocal)
                    {
                        CalendarProviderEventIdentity? identity = await TryProviderWriteAsync(
                            () => provider.CreateEventAsync(accountId, calendar.ExternalId, CopyEvent(localEvent), cancellationToken),
                            localEvent.Id, mapping.ExternalEventId);
                        if (identity is null)
                            continue;
                        mapping.ExternalEventId = identity.ExternalEventId;
                        mapping.ExternalVersion = identity.ExternalVersion;
                        mapping.LastSyncedAt = now;
                        mapping.LastSyncedLocalVersion = GetLocalVersion(localEvent);
                        await sync.SaveMappingAsync(mapping, cancellationToken);
                        exported++;
                    }
                    else if (resolution == CalendarConflictResolution.PreferRemote)
                    {
                        await events.DeleteAsync(localEvent.Id, cancellationToken);
                        await sync.DeleteMappingAsync(mapping, cancellationToken);
                        deleted++;
                    }
                    else
                    {
                        requiresManualResolution = true;
                        unresolvedConflictEventIds.Add(localEvent.Id);
                    }
                    items.Add(new CalendarSynchronizationItemResult(
                        localEvent.Id, mapping.ExternalEventId, CalendarSynchronizationOutcome.Conflict));
                }
                else
                {
                    if (localEvent is not null)
                        await events.DeleteAsync(localEvent.Id, cancellationToken);
                    await sync.DeleteMappingAsync(mapping, cancellationToken);
                    deleted++;
                    items.Add(new CalendarSynchronizationItemResult(mapping.InternalEventId, mapping.ExternalEventId, CalendarSynchronizationOutcome.Deleted));
                }
            }
        }

        IReadOnlyList<CalendarEvent> localEvents = await events.GetAllAsync(cancellationToken);
        if (provider.SupportsEventWrites)
        {
            foreach (CalendarEvent localEvent in localEvents.Where(item =>
                         (item.CalendarId == CalendarIdentity.LocalCalendarId || item.CalendarId == calendar.Id) &&
                         !mappedInternalIds.Contains(item.Id)))
            {
                CalendarProviderEventIdentity? identity = await TryProviderWriteAsync(
                    () => provider.CreateEventAsync(accountId, calendar.ExternalId, CopyEvent(localEvent), cancellationToken),
                    localEvent.Id, null);
                if (identity is null)
                    continue;
                CalendarEventMapping newMapping = new()
                {
                    InternalEventId = localEvent.Id,
                    Provider = calendar.Provider,
                    AccountId = accountId,
                    CalendarId = calendar.Id,
                    ExternalEventId = identity.ExternalEventId,
                    ExternalVersion = identity.ExternalVersion,
                    LastSyncedAt = now,
                    LastSyncedLocalVersion = GetLocalVersion(localEvent),
                };
                await sync.SaveMappingAsync(newMapping, cancellationToken);
                mappedInternalIds.Add(localEvent.Id);
                exported++;
                items.Add(new CalendarSynchronizationItemResult(localEvent.Id, identity.ExternalEventId, CalendarSynchronizationOutcome.Created));
            }

            foreach (CalendarEventMapping mapping in mappings)
            {
                if (unresolvedConflictEventIds.Contains(mapping.InternalEventId))
                    continue;
                CalendarEvent? localEvent = await events.GetByIdAsync(mapping.InternalEventId, cancellationToken);
                if (localEvent is null || !IsLocallyChanged(localEvent, mapping))
                    continue;

                CalendarProviderEventIdentity? identity = await TryProviderWriteAsync(
                    () => provider.UpdateEventAsync(accountId, calendar.ExternalId, mapping.ExternalEventId,
                        mapping.ExternalVersion, CopyEvent(localEvent), cancellationToken),
                    localEvent.Id, mapping.ExternalEventId);
                if (identity is null)
                    continue;
                mapping.ExternalVersion = identity.ExternalVersion;
                mapping.LastSyncedAt = now;
                mapping.LastSyncedLocalVersion = GetLocalVersion(localEvent);
                await sync.SaveMappingAsync(mapping, cancellationToken);
                updated++;
                items.Add(new CalendarSynchronizationItemResult(localEvent.Id, mapping.ExternalEventId, CalendarSynchronizationOutcome.Updated));
            }
        }

        if (failed == 0 && !requiresManualResolution)
        {
            IReadOnlyList<PendingCalendarOperation> pendingUpserts =
                await sync.GetPendingOperationsAsync(calendar.Id, cancellationToken);
            foreach (PendingCalendarOperation operation in pendingUpserts.Where(item => item.Type == PendingCalendarOperationType.Upsert))
            {
                CalendarEvent? synchronizedEvent = await events.GetByIdAsync(operation.InternalEventId, cancellationToken);
                IReadOnlyList<CalendarEventMapping> eventMappings = await sync.GetMappingsAsync(operation.InternalEventId, cancellationToken);
                CalendarEventMapping? eventMapping = eventMappings.SingleOrDefault(item => item.CalendarId == calendar.Id);
                if (synchronizedEvent is null ||
                    (eventMapping?.LastSyncedAt is DateTime lastSynced && synchronizedEvent.UpdatedAt <= lastSynced))
                    await sync.DeletePendingOperationAsync(operation.Id, cancellationToken);
            }
            await sync.SaveStateAsync(new CalendarSyncState
            {
                CalendarId = calendar.Id,
                Cursor = page.Cursor,
                LastSyncedAt = now,
                LastError = null,
            }, cancellationToken);
        }
        else if (failed > 0)
        {
            await sync.SaveStateAsync(new CalendarSyncState
            {
                CalendarId = calendar.Id,
                Cursor = currentState?.Cursor,
                LastSyncedAt = currentState?.LastSyncedAt,
                LastError = failures.FirstOrDefault()?.Error ?? "Calendar synchronization failed.",
            }, cancellationToken);
        }

        return new CalendarSynchronizationResult(imported + exported, updated, deleted, skipped, failed, conflicts,
            failed == 0 && !requiresManualResolution ? page.Cursor : currentState?.Cursor, failures, items, exported)
        {
            HasUnresolvedConflicts = requiresManualResolution,
        };

        async Task<CalendarProviderEventIdentity?> TryProviderWriteAsync(
            Func<Task<CalendarProviderEventIdentity>> write, Guid? internalId, string? externalId)
        {
            PendingCalendarOperation? pending = null;
            if (internalId is Guid eventId)
            {
                pending = new PendingCalendarOperation
                {
                    Id = Guid.NewGuid(),
                    CalendarId = calendar.Id,
                    InternalEventId = eventId,
                    Type = PendingCalendarOperationType.Upsert,
                    CreatedAt = DateTime.UtcNow,
                };
                await sync.SavePendingOperationAsync(pending, cancellationToken);
            }
            try
            {
                CalendarProviderEventIdentity identity = await write();
                return identity;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                if (pending is not null)
                {
                    pending.LastError = ex.Message;
                    await sync.SavePendingOperationAsync(pending, cancellationToken);
                }
                failures.Add(new CalendarSynchronizationFailure(internalId, externalId, ex.Message));
                items.Add(new CalendarSynchronizationItemResult(
                    internalId, externalId, CalendarSynchronizationOutcome.Failed, ex.Message));
                return null;
            }
        }
    }

    private static CalendarSynchronizationResult Result(int skipped = 0, int deleted = 0) =>
        new(0, 0, deleted, skipped, 0, 0, null, [], []);

    private static CalendarEventMapping NewMapping(
        Guid internalEventId,
        Calendar calendar,
        Guid accountId,
        CalendarProviderChange change,
        DateTime now) => new()
        {
            InternalEventId = internalEventId,
            Provider = calendar.Provider,
            AccountId = accountId,
            CalendarId = calendar.Id,
            ExternalEventId = change.ExternalEventId,
            ExternalVersion = change.ExternalVersion,
            LastSyncedAt = now,
            LastSyncedLocalVersion = change.Event is null ? null : GetLocalVersion(change.Event),
        };

    private static bool IsLocallyChanged(CalendarEvent calendarEvent, CalendarEventMapping mapping) =>
        mapping.LastSyncedLocalVersion is not null
            ? mapping.LastSyncedLocalVersion != GetLocalVersion(calendarEvent)
            : mapping.LastSyncedAt is DateTime lastSynced && calendarEvent.UpdatedAt > lastSynced;

    private static string GetLocalVersion(CalendarEvent calendarEvent)
    {
        string snapshot = string.Join('\u001f', calendarEvent.Title, calendarEvent.Description ?? string.Empty,
            calendarEvent.Location ?? string.Empty, calendarEvent.StartTime.Ticks, calendarEvent.EndTime.Ticks,
            calendarEvent.IsAllDay);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
    }

    private static CalendarEvent CopyEvent(CalendarEvent source) => new()
    {
        Id = source.Id,
        CalendarId = source.CalendarId,
        Title = source.Title,
        Description = source.Description,
        Location = source.Location,
        StartTime = source.StartTime,
        EndTime = source.EndTime,
        IsAllDay = source.IsAllDay,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
    };

    private static void ApplyProviderFields(CalendarEvent destination, CalendarEvent source)
    {
        destination.Title = source.Title;
        destination.Description = source.Description;
        destination.Location = source.Location;
        destination.StartTime = source.StartTime;
        destination.EndTime = source.EndTime;
        destination.IsAllDay = source.IsAllDay;
    }
}
