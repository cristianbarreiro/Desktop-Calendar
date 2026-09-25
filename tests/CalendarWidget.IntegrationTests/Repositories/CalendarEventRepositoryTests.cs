using CalendarWidget.Core.Entities;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for <see cref="EfCalendarEventRepository"/> against a real SQLite database.
/// </summary>
public sealed class CalendarEventRepositoryTests
{
    private static CalendarEvent MakeEvent(DateTime start, DateTime end, string title = "Test Event") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        StartTime = start,
        EndTime = end,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_CreatesEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 1, 9, 0, 0), new DateTime(2026, 9, 1, 10, 0, 0));
        await repo.AddAsync(ev);

        CalendarEvent? retrieved = await repo.GetByIdAsync(ev.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be(ev.Title);
        retrieved.StartTime.Should().Be(ev.StartTime);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 2, 8, 0, 0), new DateTime(2026, 9, 2, 9, 0, 0));
        await repo.AddAsync(ev);

        CalendarEvent? result = await repo.GetByIdAsync(ev.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(ev.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        CalendarEvent? result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── Date range overlap ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByDateRangeAsync_EventCompletelyInsideRange_ReturnsEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 10:00–11:00, Range: 09:00–12:00
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 10, 0, 0), new DateTime(2026, 9, 10, 11, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 9, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().ContainSingle(e => e.Id == ev.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventStartsBeforeRangeEndsInside_ReturnsEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 08:00–10:30, Range: 10:00–12:00
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 8, 0, 0), new DateTime(2026, 9, 10, 10, 30, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().ContainSingle(e => e.Id == ev.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventStartsInsideRangeEndsAfter_ReturnsEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 11:30–13:00, Range: 10:00–12:00
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 11, 30, 0), new DateTime(2026, 9, 10, 13, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().ContainSingle(e => e.Id == ev.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventSpansEntireRange_ReturnsEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 08:00–14:00, Range: 10:00–12:00
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 8, 0, 0), new DateTime(2026, 9, 10, 14, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().ContainSingle(e => e.Id == ev.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventCompletelyOutsideRange_ReturnsEmpty()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 14:00–15:00, Range: 10:00–12:00
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 14, 0, 0), new DateTime(2026, 9, 10, 15, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventEndsExactlyAtRangeStart_ReturnsEmpty()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 08:00–10:00, Range: 10:00–12:00 — EndTime == rangeStart, not overlapping
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 8, 0, 0), new DateTime(2026, 9, 10, 10, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_EventStartsExactlyAtRangeEnd_ReturnsEmpty()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        // Event: 12:00–13:00, Range: 10:00–12:00 — StartTime == rangeEnd, not overlapping
        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 10, 12, 0, 0), new DateTime(2026, 9, 10, 13, 0, 0));
        await repo.AddAsync(ev);

        IReadOnlyList<CalendarEvent> results = await repo.GetByDateRangeAsync(
            new DateTime(2026, 9, 10, 10, 0, 0),
            new DateTime(2026, 9, 10, 12, 0, 0));

        results.Should().BeEmpty();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 5, 9, 0, 0), new DateTime(2026, 9, 5, 10, 0, 0));
        await repo.AddAsync(ev);

        ev.Title = "Updated Title";
        ev.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(ev);

        CalendarEvent? retrieved = await repo.GetByIdAsync(ev.Id);
        retrieved!.Title.Should().Be("Updated Title");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        CalendarEvent ev = MakeEvent(new DateTime(2026, 9, 6, 9, 0, 0), new DateTime(2026, 9, 6, 10, 0, 0));
        await repo.AddAsync(ev);

        await repo.DeleteAsync(ev.Id);

        CalendarEvent? retrieved = await repo.GetByIdAsync(ev.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository repo = new(db.Context);

        Func<Task> act = () => repo.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }
}
