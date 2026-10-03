using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents a calendar event.
/// </summary>
public sealed class CalendarEvent
{
    /// <summary>Gets or sets the calendar that owns the event.</summary>
    public Guid CalendarId { get; set; } = CalendarIdentity.LocalCalendarId;

    /// <summary>Gets or sets the unique identifier of the event.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the event title.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets the optional event description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional event location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the event start timestamp (UTC).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Gets or sets the event end timestamp (UTC).</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Gets or sets whether the event spans the entire day.</summary>
    public bool IsAllDay { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Validates the event's domain invariants.
    /// Throws <see cref="DomainValidationException"/> if any rule is violated.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainValidationException("Event title is required.");

        if (Title.Length > 200)
            throw new DomainValidationException("Event title cannot exceed 200 characters.");

        if (Description is not null && Description.Length > 2000)
            throw new DomainValidationException("Event description cannot exceed 2000 characters.");

        if (Location is not null && Location.Length > 500)
            throw new DomainValidationException("Event location cannot exceed 500 characters.");

        if (CalendarId == Guid.Empty)
            throw new DomainValidationException("Event calendar is required.");

        if (EndTime < StartTime)
            throw new DomainValidationException("Event end time cannot be earlier than start time.");
    }
}
