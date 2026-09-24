namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents a user note.
/// </summary>
public sealed class Note
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
