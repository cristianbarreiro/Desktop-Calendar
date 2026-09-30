using CalendarWidget.Core.Exceptions;

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

    /// <summary>
    /// Validates the note's domain invariants.
    /// Throws <see cref="DomainValidationException"/> if any rule is violated.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainValidationException("Note title is required.");

        if (Title.Length > 200)
            throw new DomainValidationException("Note title cannot exceed 200 characters.");

        if (Content is not null && Content.Length > 50_000)
            throw new DomainValidationException("Note content cannot exceed 50,000 characters.");
    }
}
