using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;

namespace CalendarWidget.Core.Models;

/// <summary>
/// Result report from an application data import operation.
/// </summary>
public sealed record DataImportResult
{
    /// <summary>
    /// Gets a value indicating whether the import operation completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error description if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the count of new calendar events imported into the database.
    /// </summary>
    public int EventsImported { get; init; }

    /// <summary>
    /// Gets the count of existing calendar events skipped to avoid overwriting.
    /// </summary>
    public int EventsSkipped { get; init; }

    /// <summary>
    /// Gets the count of new notes imported into the database.
    /// </summary>
    public int NotesImported { get; init; }

    /// <summary>
    /// Gets the count of existing notes skipped to avoid overwriting.
    /// </summary>
    public int NotesSkipped { get; init; }

    public int CalendarAccountsImported { get; init; }
    public int CalendarAccountsSkipped { get; init; }
    public int CalendarsImported { get; init; }
    public int CalendarsSkipped { get; init; }
    public int EventMappingsImported { get; init; }
    public int EventMappingsSkipped { get; init; }
    public int SyncStatesImported { get; init; }
    public int SyncStatesSkipped { get; init; }
}

/// <summary>
/// Top-level schema for application data backup exports and imports.
/// </summary>
public sealed record AppBackupData
{
    /// <summary>
    /// Backup file format version.
    /// </summary>
    public int Version { get; init; } = 6;

    /// <summary>
    /// Timestamp when this backup was exported (UTC).
    /// </summary>
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Exported application user preferences.
    /// </summary>
    public UserSettings? Settings { get; init; }

    /// <summary>
    /// Exported calendar events.
    /// </summary>
    public IReadOnlyList<CalendarEventBackupDto> Events { get; init; } = [];

    /// <summary>
    /// Exported notes.
    /// </summary>
    public IReadOnlyList<NoteBackupDto> Notes { get; init; } = [];

    public IReadOnlyList<CalendarAccountBackupDto> CalendarAccounts { get; init; } = [];

    public IReadOnlyList<CalendarBackupDto> Calendars { get; init; } = [];

    public IReadOnlyList<CalendarEventMappingBackupDto> EventMappings { get; init; } = [];

    public IReadOnlyList<CalendarSyncStateBackupDto> CalendarSyncStates { get; init; } = [];
    public IReadOnlyList<PendingCalendarOperationBackupDto> PendingCalendarOperations { get; init; } = [];
}

/// <summary>
/// Portable data transfer object for a calendar event in a backup file.
/// </summary>
public sealed record CalendarEventBackupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public Guid CalendarId { get; init; } = CalendarIdentity.LocalCalendarId;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public bool IsAllDay { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record CalendarAccountBackupDto
{
    public Guid Id { get; init; }
    public CalendarProvider Provider { get; init; }
    public string ProviderAccountId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsConnected { get; init; } = true;
    public DateTime CreatedAt { get; init; }
}

public sealed record CalendarBackupDto
{
    public Guid Id { get; init; }
    public CalendarProvider Provider { get; init; }
    public Guid? AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CalendarEventMappingBackupDto
{
    public Guid InternalEventId { get; init; }
    public CalendarProvider Provider { get; init; }
    public Guid AccountId { get; init; }
    public Guid CalendarId { get; init; }
    public string ExternalEventId { get; init; } = string.Empty;
    public string? ExternalVersion { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public string? LastSyncedLocalVersion { get; init; }
}

public sealed record CalendarSyncStateBackupDto
{
    public Guid CalendarId { get; init; }
    public string? Cursor { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public string? LastError { get; init; }
}

public sealed record PendingCalendarOperationBackupDto
{
    public Guid Id { get; init; }
    public Guid CalendarId { get; init; }
    public Guid InternalEventId { get; init; }
    public PendingCalendarOperationType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? LastError { get; init; }
}

/// <summary>
/// Portable data transfer object for a note in a backup file.
/// </summary>
public sealed record NoteBackupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
