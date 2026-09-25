using CalendarWidget.Core.Entities;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for <see cref="EfNoteRepository"/> against a real SQLite database.
/// </summary>
public sealed class NoteRepositoryTests
{
    private static Note MakeNote(string title, string content = "", DateTime? createdAt = null)
    {
        DateTime ts = createdAt ?? DateTime.UtcNow;
        return new Note
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = ts,
            UpdatedAt = ts,
        };
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsNote()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note note = MakeNote("My Note", "Some content");
        await repo.AddAsync(note);

        Note? retrieved = await repo.GetByIdAsync(note.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("My Note");
        retrieved.Content.Should().Be("Some content");
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsNote()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note note = MakeNote("Existing Note");
        await repo.AddAsync(note);

        Note? result = await repo.GetByIdAsync(note.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note? result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── Get all ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotesOrderedByCreatedAt()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        DateTime t0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Note first = MakeNote("First", createdAt: t0);
        Note second = MakeNote("Second", createdAt: t0.AddMinutes(1));
        Note third = MakeNote("Third", createdAt: t0.AddMinutes(2));

        await repo.AddAsync(third);
        await repo.AddAsync(first);
        await repo.AddAsync(second);

        IReadOnlyList<Note> results = await repo.GetAllAsync();

        results.Should().HaveCount(3);
        results[0].Title.Should().Be("First");
        results[1].Title.Should().Be("Second");
        results[2].Title.Should().Be("Third");
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatchingNotes()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Project Alpha"));
        await repo.AddAsync(MakeNote("Grocery List"));
        await repo.AddAsync(MakeNote("Alpha Study"));

        IReadOnlyList<Note> results = await repo.SearchAsync("alpha");

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(n => n.Title.ToUpperInvariant().Should().Contain("ALPHA"));
    }

    [Fact]
    public async Task SearchAsync_ByContent_ReturnsMatchingNotes()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Note A", "contains the keyword here"));
        await repo.AddAsync(MakeNote("Note B", "nothing relevant"));

        IReadOnlyList<Note> results = await repo.SearchAsync("keyword");

        results.Should().ContainSingle(n => n.Title == "Note A");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsMatches()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Project Alpha"));
        await repo.AddAsync(MakeNote("Grocery List"));
        await repo.AddAsync(MakeNote("Alpha Study"));

        IReadOnlyList<Note> upper = await repo.SearchAsync("ALPHA");
        IReadOnlyList<Note> mixed = await repo.SearchAsync("AlPhA");

        upper.Should().HaveCount(2);
        mixed.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_SubstringMatch_ReturnsMatches()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Alphabetical Order"));
        await repo.AddAsync(MakeNote("Unrelated"));

        IReadOnlyList<Note> results = await repo.SearchAsync("pha");

        results.Should().ContainSingle(n => n.Title == "Alphabetical Order");
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmpty()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Note One"));
        await repo.AddAsync(MakeNote("Note Two"));

        IReadOnlyList<Note> results = await repo.SearchAsync("zzznomatch");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsAllNotes()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Note One"));
        await repo.AddAsync(MakeNote("Note Two"));

        IReadOnlyList<Note> results = await repo.SearchAsync(string.Empty);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsAllNotes()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        await repo.AddAsync(MakeNote("Note One"));
        await repo.AddAsync(MakeNote("Note Two"));

        IReadOnlyList<Note> results = await repo.SearchAsync("   ");

        results.Should().HaveCount(2);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note note = MakeNote("Original Title", "Original content");
        await repo.AddAsync(note);

        DateTime updatedAt = note.CreatedAt.AddSeconds(10);
        note.Title = "Updated Title";
        note.Content = "Updated content";
        note.UpdatedAt = updatedAt;
        await repo.UpdateAsync(note);

        Note? retrieved = await repo.GetByIdAsync(note.Id);
        retrieved!.Title.Should().Be("Updated Title");
        retrieved.Content.Should().Be("Updated content");
        retrieved.UpdatedAt.Should().Be(updatedAt);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesNote()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note note = MakeNote("To Delete");
        await repo.AddAsync(note);

        await repo.DeleteAsync(note.Id);

        Note? retrieved = await repo.GetByIdAsync(note.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Func<Task> act = () => repo.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }
}
