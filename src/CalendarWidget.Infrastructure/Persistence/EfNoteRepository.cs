using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.Infrastructure.Persistence;

/// <summary>
/// EF Core SQLite implementation of <see cref="INoteRepository"/>.
/// </summary>
public sealed class EfNoteRepository(AppDbContext context) : INoteRepository
{
    /// <inheritdoc />
    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Notes
            .AsNoTracking()
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Note>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync(cancellationToken);

        // SQLite LIKE is case-insensitive for ASCII characters, which satisfies the spec requirement.
        string pattern = $"%{query}%";

        return await context.Notes
            .AsNoTracking()
            .Where(n => EF.Functions.Like(n.Title, pattern) || EF.Functions.Like(n.Content, pattern))
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Note> AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        context.Notes.Add(note);
        await context.SaveChangesAsync(cancellationToken);
        return note;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        context.Notes.Update(note);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Note? entity = await context.Notes
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (entity is null)
            return;

        context.Notes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
