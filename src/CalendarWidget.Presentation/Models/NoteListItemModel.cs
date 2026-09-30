using CalendarWidget.Core.Entities;

namespace CalendarWidget.Presentation.Models;

/// <summary>
/// Immutable presentation model for displaying a note in the list.
/// </summary>
/// <param name="Id">The note identifier.</param>
/// <param name="Title">The note title.</param>
/// <param name="Content">The full note content.</param>
/// <param name="Preview">A single-line preview snippet of the note content.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="UpdatedAt">The UTC last-update timestamp.</param>
/// <param name="FormattedDate">Localized formatted update date string.</param>
public sealed record NoteListItemModel(
    Guid Id,
    string Title,
    string Content,
    string Preview,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string FormattedDate)
{
    /// <summary>
    /// Creates a <see cref="NoteListItemModel"/> from a <see cref="Note"/> entity.
    /// </summary>
    /// <param name="note">The source note entity.</param>
    /// <returns>A new <see cref="NoteListItemModel"/>.</returns>
    public static NoteListItemModel FromEntity(Note note)
    {
        string preview = string.IsNullOrWhiteSpace(note.Content)
            ? "No additional text"
            : note.Content.Trim().Replace("\r\n", " ").Replace("\n", " ");

        if (preview.Length > 80)
        {
            preview = string.Concat(preview.AsSpan(0, 77), "...");
        }

        DateTime localUpdated = note.UpdatedAt.Kind == DateTimeKind.Utc
            ? note.UpdatedAt.ToLocalTime()
            : note.UpdatedAt;

        return new NoteListItemModel(
            note.Id,
            note.Title,
            note.Content,
            preview,
            note.CreatedAt,
            note.UpdatedAt,
            localUpdated.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture));
    }
}
