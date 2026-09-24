namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents a calendar event.
/// </summary>
public sealed class CalendarEvent
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsAllDay { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
