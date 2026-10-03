using System.Text.Json;
using System.Text.Json.Serialization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>
/// Service coordinating data export, safe import, and factory reset across persistence stores.
/// </summary>
public sealed class DataManagementService : IDataManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Action<ILogger, int, int, Exception?> LogExportCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(1, "ExportCompleted"), "Exported {EventCount} events and {NoteCount} notes to backup JSON.");

    private static readonly Action<ILogger, int, int, Exception?> LogImportCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(2, "ImportCompleted"), "Imported {EventCount} events and {NoteCount} notes.");

    private static readonly Action<ILogger, Exception?> LogResetCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(3, "ResetCompleted"), "All user events and notes have been cleared.");

    private static readonly Action<ILogger, string, Exception?> LogImportFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, "ImportFailed"), "Data import failed: {Message}");

    private static readonly Action<ILogger, Exception?> LogRollbackFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(5, "RollbackFailed"), "Database transaction rollback failed during import recovery.");

    private static readonly Action<ILogger, Exception?> LogSettingsRestoreFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6, "SettingsRestoreFailed"), "Settings compensation restoration failed during import recovery.");

    private static readonly Action<ILogger, string, Exception?> LogRecoveryIncomplete =
        LoggerMessage.Define<string>(LogLevel.Critical, new EventId(7, "RecoveryIncomplete"), "Data import recovery could not be fully confirmed: {Details}");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DataManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataManagementService"/> class.
    /// </summary>
    public DataManagementService(
        IServiceScopeFactory scopeFactory,
        ISettingsService settingsService,
        ILogger<DataManagementService> logger)
    {
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? DataChanged;

    /// <inheritdoc />
    public async Task<string> ExportDataJsonAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<CalendarEvent> events = await context.CalendarEvents
            .AsNoTracking()
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        List<Note> notes = await context.Notes
            .AsNoTracking()
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);

        AppBackupData backup = new()
        {
            Version = 6,
            ExportedAt = DateTime.UtcNow,
            Settings = _settingsService.CurrentSettings,
            Events = events.Select(e => new CalendarEventBackupDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                CalendarId = e.CalendarId,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                IsAllDay = e.IsAllDay,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
            }).ToList(),
            Notes = notes.Select(n => new NoteBackupDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
            }).ToList(),
            CalendarAccounts = await context.CalendarAccounts.AsNoTracking()
                .Select(account => new CalendarAccountBackupDto
                {
                    Id = account.Id,
                    Provider = account.Provider,
                    ProviderAccountId = account.ProviderAccountId,
                    DisplayName = account.DisplayName,
                    IsConnected = account.IsConnected,
                    CreatedAt = account.CreatedAt,
                }).ToListAsync(cancellationToken),
            Calendars = await context.Calendars.AsNoTracking()
                .Select(calendar => new CalendarBackupDto
                {
                    Id = calendar.Id,
                    Provider = calendar.Provider,
                    AccountId = calendar.AccountId,
                    Name = calendar.Name,
                    ExternalId = calendar.ExternalId,
                    IsEnabled = calendar.IsEnabled,
                    CreatedAt = calendar.CreatedAt,
                }).ToListAsync(cancellationToken),
            EventMappings = await context.CalendarEventMappings.AsNoTracking()
                .Select(mapping => new CalendarEventMappingBackupDto
                {
                    InternalEventId = mapping.InternalEventId,
                    Provider = mapping.Provider,
                    AccountId = mapping.AccountId,
                    CalendarId = mapping.CalendarId,
                    ExternalEventId = mapping.ExternalEventId,
                    ExternalVersion = mapping.ExternalVersion,
                    LastSyncedAt = mapping.LastSyncedAt,
                    LastSyncedLocalVersion = mapping.LastSyncedLocalVersion,
                }).ToListAsync(cancellationToken),
            CalendarSyncStates = await context.CalendarSyncStates.AsNoTracking()
                .Select(state => new CalendarSyncStateBackupDto
                {
                    CalendarId = state.CalendarId,
                    Cursor = state.Cursor,
                    LastSyncedAt = state.LastSyncedAt,
                    LastError = state.LastError,
                }).ToListAsync(cancellationToken),
            PendingCalendarOperations = await context.PendingCalendarOperations.AsNoTracking()
                .Select(operation => new PendingCalendarOperationBackupDto
                {
                    Id = operation.Id,
                    CalendarId = operation.CalendarId,
                    InternalEventId = operation.InternalEventId,
                    Type = operation.Type,
                    CreatedAt = operation.CreatedAt,
                    LastError = operation.LastError,
                }).ToListAsync(cancellationToken),
        };

        LogExportCompleted(_logger, backup.Events.Count, backup.Notes.Count, null);
        return JsonSerializer.Serialize(backup, JsonOptions);
    }

    /// <inheritdoc />
    public async Task<DataImportResult> ImportDataJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new DataImportResult
            {
                Success = false,
                ErrorMessage = "Backup file is empty.",
            };
        }

        AppBackupData? backup;
        try
        {
            backup = JsonSerializer.Deserialize<AppBackupData>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new DataImportResult
            {
                Success = false,
                ErrorMessage = $"Invalid backup file format: {ex.Message}",
            };
        }

        if (backup is null)
        {
            return new DataImportResult
            {
                Success = false,
                ErrorMessage = "Failed to parse backup payload.",
            };
        }

        if (backup.Version is < 1 or > 6)
        {
            return new DataImportResult
            {
                Success = false,
                ErrorMessage = $"Backup version {backup.Version} is not supported.",
            };
        }

        // 1. Validate complete settings object before any mutation
        if (backup.Settings is not null)
        {
            try
            {
                backup.Settings.Validate();
            }
            catch (DomainValidationException ex)
            {
                return new DataImportResult
                {
                    Success = false,
                    ErrorMessage = $"Settings failed validation: {ex.Message}",
                };
            }
        }

        // 2. Validate all events before database mutation
        List<CalendarEvent> eventsToImport = new(backup.Events.Count);
        foreach (CalendarEventBackupDto dto in backup.Events)
        {
            CalendarEvent ev = new()
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                CalendarId = dto.CalendarId == Guid.Empty ? CalendarIdentity.LocalCalendarId : dto.CalendarId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAllDay = dto.IsAllDay,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt == default ? DateTime.UtcNow : dto.UpdatedAt,
            };

            try
            {
                ev.Validate();
            }
            catch (DomainValidationException ex)
            {
                return new DataImportResult
                {
                    Success = false,
                    ErrorMessage = $"Event '{dto.Title}' failed validation: {ex.Message}",
                };
            }

            eventsToImport.Add(ev);
        }

        List<CalendarAccount> accountsToImport = backup.CalendarAccounts.Select(dto => new CalendarAccount
        {
            Id = dto.Id,
            Provider = dto.Provider,
            ProviderAccountId = dto.ProviderAccountId,
            DisplayName = dto.DisplayName,
            IsConnected = dto.IsConnected,
            CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
        }).ToList();
        List<Calendar> calendarsToImport = backup.Calendars.Select(dto => new Calendar
        {
            Id = dto.Id,
            Provider = dto.Provider,
            AccountId = dto.AccountId,
            Name = dto.Name,
            ExternalId = dto.ExternalId,
            IsEnabled = dto.IsEnabled,
            CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
        }).ToList();
        List<CalendarEventMapping> mappingsToImport = backup.EventMappings.Select(dto => new CalendarEventMapping
        {
            InternalEventId = dto.InternalEventId,
            Provider = dto.Provider,
            AccountId = dto.AccountId,
            CalendarId = dto.CalendarId,
            ExternalEventId = dto.ExternalEventId,
            ExternalVersion = dto.ExternalVersion,
            LastSyncedAt = dto.LastSyncedAt,
            LastSyncedLocalVersion = dto.LastSyncedLocalVersion,
        }).ToList();
        List<CalendarSyncState> syncStatesToImport = backup.CalendarSyncStates.Select(dto => new CalendarSyncState
        {
            CalendarId = dto.CalendarId,
            Cursor = dto.Cursor,
            LastSyncedAt = dto.LastSyncedAt,
            LastError = dto.LastError,
        }).ToList();
        List<PendingCalendarOperation> pendingOperationsToImport = backup.PendingCalendarOperations.Select(dto => new PendingCalendarOperation
        {
            Id = dto.Id,
            CalendarId = dto.CalendarId,
            InternalEventId = dto.InternalEventId,
            Type = dto.Type,
            CreatedAt = dto.CreatedAt,
            LastError = dto.LastError,
        }).ToList();

        // 3. Validate all notes before database mutation
        List<Note> notesToImport = new(backup.Notes.Count);
        foreach (NoteBackupDto dto in backup.Notes)
        {
            Note note = new()
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt == default ? DateTime.UtcNow : dto.UpdatedAt,
            };

            try
            {
                note.Validate();
            }
            catch (DomainValidationException ex)
            {
                return new DataImportResult
                {
                    Success = false,
                    ErrorMessage = $"Note '{dto.Title}' failed validation: {ex.Message}",
                };
            }

            notesToImport.Add(note);
        }

        // 4. Atomically persist records with explicit rollback and compensation on failure
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        UserSettings? originalSettings = backup.Settings is not null ? _settingsService.CurrentSettings.Clone() : null;
        bool settingsAttempted = false;

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction;
        try
        {
            transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogImportFailed(_logger, ex.Message, ex);
            return new DataImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}",
            };
        }

        await using (transaction)
        {
            try
            {
                HashSet<Guid> existingEventIds = (await context.CalendarEvents
                    .Select(e => e.Id)
                    .ToListAsync(cancellationToken))
                    .ToHashSet();

                HashSet<Guid> existingNoteIds = (await context.Notes
                    .Select(n => n.Id)
                    .ToListAsync(cancellationToken))
                    .ToHashSet();

                HashSet<Guid> existingAccountIds = (await context.CalendarAccounts.Select(account => account.Id)
                    .ToListAsync(cancellationToken)).ToHashSet();
                HashSet<Guid> existingCalendarIds = (await context.Calendars.Select(calendar => calendar.Id)
                    .ToListAsync(cancellationToken)).ToHashSet();

                Dictionary<Guid, CalendarAccount> accountsById = await context.CalendarAccounts.AsNoTracking()
                    .ToDictionaryAsync(account => account.Id, cancellationToken);
                foreach (CalendarAccount account in accountsToImport)
                    accountsById.TryAdd(account.Id, account);

                Dictionary<Guid, Calendar> calendarsById = await context.Calendars.AsNoTracking()
                    .ToDictionaryAsync(calendar => calendar.Id, cancellationToken);
                foreach (Calendar calendar in calendarsToImport)
                    calendarsById.TryAdd(calendar.Id, calendar);

                foreach (Calendar calendar in calendarsToImport)
                {
                    if (calendar.AccountId is Guid accountId &&
                        (!accountsById.TryGetValue(accountId, out CalendarAccount? account) || account.Provider != calendar.Provider))
                        throw new InvalidDataException("Calendar backup entry references an unknown or mismatched account.");
                }

                HashSet<Guid> availableEventIds = eventsToImport.Select(ev => ev.Id).Concat(existingEventIds).ToHashSet();
                foreach (CalendarEvent ev in eventsToImport)
                {
                    if (!calendarsById.ContainsKey(ev.CalendarId))
                        throw new InvalidDataException("Event backup entry references an unknown calendar.");
                }

                foreach (CalendarEventMapping mapping in mappingsToImport)
                {
                    if (!availableEventIds.Contains(mapping.InternalEventId) ||
                        !accountsById.TryGetValue(mapping.AccountId, out CalendarAccount? account) ||
                        !calendarsById.TryGetValue(mapping.CalendarId, out Calendar? calendar) ||
                        account.Provider != mapping.Provider || calendar.Provider != mapping.Provider ||
                        calendar.AccountId != mapping.AccountId)
                        throw new InvalidDataException("Event mapping backup entry references mismatched event, account, or calendar data.");
                }

                foreach (CalendarSyncState state in syncStatesToImport)
                {
                    if (!calendarsById.ContainsKey(state.CalendarId))
                        throw new InvalidDataException("Sync state backup entry references an unknown calendar.");
                }

                foreach (PendingCalendarOperation operation in pendingOperationsToImport)
                {
                    if (!calendarsById.ContainsKey(operation.CalendarId) || !availableEventIds.Contains(operation.InternalEventId))
                        throw new InvalidDataException("Pending operation backup entry references an unknown event or calendar.");
                }

                int eventsImported = 0;
                int eventsSkipped = 0;
                foreach (CalendarEvent ev in eventsToImport)
                {
                    if (existingEventIds.Contains(ev.Id))
                    {
                        eventsSkipped++;
                    }
                    else
                    {
                        context.CalendarEvents.Add(ev);
                        eventsImported++;
                    }
                }

                int notesImported = 0;
                int notesSkipped = 0;
                foreach (Note n in notesToImport)
                {
                    if (existingNoteIds.Contains(n.Id))
                    {
                        notesSkipped++;
                    }
                    else
                    {
                        context.Notes.Add(n);
                        notesImported++;
                    }
                }

                int accountsImported = 0;
                int accountsSkipped = 0;
                foreach (CalendarAccount account in accountsToImport)
                {
                    if (existingAccountIds.Add(account.Id))
                    {
                        context.CalendarAccounts.Add(account);
                        accountsImported++;
                    }
                    else accountsSkipped++;
                }

                int calendarsImported = 0;
                int calendarsSkipped = 0;
                foreach (Calendar calendar in calendarsToImport)
                {
                    if (existingCalendarIds.Add(calendar.Id))
                    {
                        context.Calendars.Add(calendar);
                        calendarsImported++;
                    }
                    else calendarsSkipped++;
                }

                int mappingsImported = 0;
                int mappingsSkipped = 0;
                foreach (CalendarEventMapping mapping in mappingsToImport)
                {
                    bool duplicate = await context.CalendarEventMappings.AnyAsync(existing =>
                        existing.InternalEventId == mapping.InternalEventId &&
                        existing.Provider == mapping.Provider &&
                        existing.AccountId == mapping.AccountId && existing.CalendarId == mapping.CalendarId ||
                        existing.Provider == mapping.Provider && existing.AccountId == mapping.AccountId &&
                        existing.CalendarId == mapping.CalendarId && existing.ExternalEventId == mapping.ExternalEventId,
                        cancellationToken);
                    if (!duplicate)
                    {
                        context.CalendarEventMappings.Add(mapping);
                        mappingsImported++;
                    }
                    else mappingsSkipped++;
                }

                int syncStatesImported = 0;
                int syncStatesSkipped = 0;
                foreach (CalendarSyncState state in syncStatesToImport)
                {
                    if (!await context.CalendarSyncStates.AnyAsync(existing => existing.CalendarId == state.CalendarId, cancellationToken))
                    {
                        context.CalendarSyncStates.Add(state);
                        syncStatesImported++;
                    }
                    else syncStatesSkipped++;
                }

                foreach (PendingCalendarOperation operation in pendingOperationsToImport)
                {
                    if (!await context.PendingCalendarOperations.AnyAsync(existing => existing.Id == operation.Id, cancellationToken))
                        context.PendingCalendarOperations.Add(operation);
                }

                await context.SaveChangesAsync(cancellationToken);

                // Transaction & Compensation Strategy:
                // Settings persistence is executed before committing the SQLite transaction.
                // If Settings persistence fails, the SQLite transaction has not been committed and is cleanly rolled back.
                // If SQLite Commit fails, the SQLite transaction is aborted/rolled back, and Settings are compensated by restoring originalSettings.
                if (backup.Settings is not null)
                {
                    settingsAttempted = true;
                    await _settingsService.SaveSettingsAsync(backup.Settings, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                LogImportCompleted(_logger, eventsImported, notesImported, null);
                DataChanged?.Invoke(this, EventArgs.Empty);

                return new DataImportResult
                {
                    Success = true,
                    EventsImported = eventsImported,
                    EventsSkipped = eventsSkipped,
                    NotesImported = notesImported,
                    NotesSkipped = notesSkipped,
                    CalendarAccountsImported = accountsImported,
                    CalendarAccountsSkipped = accountsSkipped,
                    CalendarsImported = calendarsImported,
                    CalendarsSkipped = calendarsSkipped,
                    EventMappingsImported = mappingsImported,
                    EventMappingsSkipped = mappingsSkipped,
                    SyncStatesImported = syncStatesImported,
                    SyncStatesSkipped = syncStatesSkipped,
                };
            }
            catch (Exception ex)
            {
                // Recovery and compensation:
                // Use CancellationToken.None so that mandatory cleanup (rollback and settings restoration)
                // is executed even if the operation was cancelled by the caller.
                bool rollbackFailed = false;
                Exception? rollbackException = null;

                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rbEx)
                {
                    rollbackFailed = true;
                    rollbackException = rbEx;
                    LogRollbackFailed(_logger, rbEx);
                }

                bool settingsRestoreFailed = false;
                Exception? settingsRestoreException = null;

                if (settingsAttempted && originalSettings is not null)
                {
                    try
                    {
                        await _settingsService.SaveSettingsAsync(originalSettings, CancellationToken.None);
                    }
                    catch (Exception srEx)
                    {
                        settingsRestoreFailed = true;
                        settingsRestoreException = srEx;
                        LogSettingsRestoreFailed(_logger, srEx);
                    }
                }

                string errorMessage;
                if (rollbackFailed && settingsRestoreFailed)
                {
                    string details = $"Database rollback failed ({rollbackException?.Message}) and settings restoration failed ({settingsRestoreException?.Message}).";
                    LogRecoveryIncomplete(_logger, details, ex);
                    errorMessage = $"Import failed: {ex.Message}. Database rollback and complete settings restoration could not be confirmed.";
                }
                else if (rollbackFailed)
                {
                    string details = $"Database rollback failed ({rollbackException?.Message}).";
                    LogRecoveryIncomplete(_logger, details, ex);
                    errorMessage = $"Import failed: {ex.Message}. Database rollback could not be confirmed.";
                }
                else if (settingsRestoreFailed)
                {
                    string details = $"Settings restoration failed ({settingsRestoreException?.Message}).";
                    LogRecoveryIncomplete(_logger, details, ex);
                    errorMessage = $"Import failed: {ex.Message}. Complete settings restoration could not be confirmed.";
                }
                else
                {
                    LogImportFailed(_logger, ex.Message, ex);
                    errorMessage = $"Import failed: {ex.Message}";
                }

                return new DataImportResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task ResetAllDataAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.CalendarEvents.RemoveRange(context.CalendarEvents);
        context.Notes.RemoveRange(context.Notes);
        context.CalendarEventMappings.RemoveRange(context.CalendarEventMappings);
        context.CalendarSyncStates.RemoveRange(context.CalendarSyncStates);
        context.PendingCalendarOperations.RemoveRange(context.PendingCalendarOperations);
        context.Calendars.RemoveRange(context.Calendars.Where(calendar => calendar.Id != CalendarIdentity.LocalCalendarId));
        context.CalendarAccounts.RemoveRange(context.CalendarAccounts);
        await context.SaveChangesAsync(cancellationToken);

        LogResetCompleted(_logger, null);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
