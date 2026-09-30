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

    private static readonly Action<ILogger, Exception?> LogInvalidImportedSettings =
        LoggerMessage.Define(LogLevel.Warning, new EventId(4, "InvalidImportedSettings"), "Imported settings were invalid; skipping settings update.");

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
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Settings = _settingsService.CurrentSettings,
            Events = events.Select(e => new CalendarEventBackupDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
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

        // Validate all events before database mutation
        List<CalendarEvent> eventsToImport = new(backup.Events.Count);
        foreach (CalendarEventBackupDto dto in backup.Events)
        {
            CalendarEvent ev = new()
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                Title = dto.Title,
                Description = dto.Description,
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

        // Validate all notes before database mutation
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

        // Atomically persist records
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        HashSet<Guid> existingEventIds = (await context.CalendarEvents
            .Select(e => e.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        HashSet<Guid> existingNoteIds = (await context.Notes
            .Select(n => n.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

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

        await context.SaveChangesAsync(cancellationToken);

        // Optionally apply imported settings if present
        if (backup.Settings is not null)
        {
            try
            {
                backup.Settings.Validate();
                await _settingsService.SaveSettingsAsync(backup.Settings, cancellationToken);
            }
            catch (DomainValidationException ex)
            {
                LogInvalidImportedSettings(_logger, ex);
            }
        }

        LogImportCompleted(_logger, eventsImported, notesImported, null);
        DataChanged?.Invoke(this, EventArgs.Empty);

        return new DataImportResult
        {
            Success = true,
            EventsImported = eventsImported,
            EventsSkipped = eventsSkipped,
            NotesImported = notesImported,
            NotesSkipped = notesSkipped,
        };
    }

    /// <inheritdoc />
    public async Task ResetAllDataAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.CalendarEvents.RemoveRange(context.CalendarEvents);
        context.Notes.RemoveRange(context.Notes);
        await context.SaveChangesAsync(cancellationToken);

        LogResetCompleted(_logger, null);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
