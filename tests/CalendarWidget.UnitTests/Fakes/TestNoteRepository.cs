using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// In-memory fake implementation of <see cref="INoteRepository"/> for unit tests.
/// </summary>
public sealed class TestNoteRepository : INoteRepository
{
    private readonly List<Note> _notes = [];

    public Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));

    public Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Note> result = _notes.OrderBy(n => n.CreatedAt).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Note>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllAsync(cancellationToken);

        IReadOnlyList<Note> result = _notes
            .Where(n => n.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        n.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.CreatedAt)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Note> AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        _notes.Add(note);
        return Task.FromResult(note);
    }

    public Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        int index = _notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0)
            _notes[index] = note;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _notes.RemoveAll(n => n.Id == id);
        return Task.CompletedTask;
    }

    /// <summary>Directly seeds a note without going through the repository contract.</summary>
    public void Seed(Note note) => _notes.Add(note);

    /// <summary>Returns all stored notes.</summary>
    public IReadOnlyList<Note> All => _notes.AsReadOnly();
}
