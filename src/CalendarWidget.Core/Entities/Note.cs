namespace CalendarWidget.Core.Entities;

/// <summary>
/// Represents a user note.
/// </summary>
public sealed class Note
{
    /// <summary>Gets or sets the unique identifier of the note.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the note title.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets the note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}
