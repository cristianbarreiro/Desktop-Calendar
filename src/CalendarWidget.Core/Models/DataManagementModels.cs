using CalendarWidget.Core.Entities;

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
}

/// <summary>
/// Top-level schema for application data backup exports and imports.
/// </summary>
public sealed record AppBackupData
{
    /// <summary>
    /// Backup file format version.
    /// </summary>
    public int Version { get; init; } = 1;

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
}

/// <summary>
/// Portable data transfer object for a calendar event in a backup file.
/// </summary>
public sealed record CalendarEventBackupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public bool IsAllDay { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
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
