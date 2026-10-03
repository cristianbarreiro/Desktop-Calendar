using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// Persisted synchronization checkpoint for a calendar.
/// Cursor is provider-owned opaque text and has no Core interpretation.
/// </summary>
public sealed class CalendarSyncState
{
    public Guid CalendarId { get; set; }
    public string? Cursor { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastError { get; set; }

    public void Validate()
    {
        if (CalendarId == Guid.Empty)
            throw new DomainValidationException("Sync state calendar identifier is required.");
    }
}
