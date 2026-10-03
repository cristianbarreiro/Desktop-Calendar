using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// Maps a local event to its identity and version in one external calendar.
/// </summary>
public sealed class CalendarEventMapping
{
    public Guid InternalEventId { get; set; }
    public CalendarProvider Provider { get; set; }
    public Guid AccountId { get; set; }
    public Guid CalendarId { get; set; }
    public required string ExternalEventId { get; set; }
    public string? ExternalVersion { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastSyncedLocalVersion { get; set; }

    public void Validate()
    {
        if (InternalEventId == Guid.Empty || AccountId == Guid.Empty || CalendarId == Guid.Empty)
            throw new DomainValidationException("Event mapping identifiers are required.");

        if (!Enum.IsDefined(Provider) || Provider == CalendarProvider.Local)
            throw new DomainValidationException("Local events do not need external mappings.");

        if (string.IsNullOrWhiteSpace(ExternalEventId) || ExternalEventId.Length > 500)
            throw new DomainValidationException("External event identifier is required and cannot exceed 500 characters.");
    }
}
