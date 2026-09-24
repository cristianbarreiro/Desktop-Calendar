namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents a calendar event.
/// </summary>
public sealed class CalendarEvent
{
    /// <summary>Gets or sets the unique identifier of the event.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the event title.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets the optional event description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the event start timestamp.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Gets or sets the event end timestamp.</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Gets or sets whether the event spans the entire day.</summary>
    public bool IsAllDay { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}
