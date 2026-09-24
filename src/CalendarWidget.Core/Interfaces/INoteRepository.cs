using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Repository for note persistence operations.
/// </summary>
public interface INoteRepository
{
    /// <summary>Gets a note by its identifier.</summary>
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Gets all notes.</summary>
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Searches notes by query text.</summary>
    Task<IReadOnlyList<Note>> SearchAsync(string query, CancellationToken cancellationToken = default);
    /// <summary>Adds a note.</summary>
    Task<Note> AddAsync(Note note, CancellationToken cancellationToken = default);
    /// <summary>Updates a note.</summary>
    Task UpdateAsync(Note note, CancellationToken cancellationToken = default);
    /// <summary>Deletes a note by identifier.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
