using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// In-memory fake implementation of <see cref="ICalendarEventRepository"/> for unit tests.
/// </summary>
public sealed class TestCalendarEventRepository : ICalendarEventRepository
{
    private readonly List<CalendarEvent> _events = [];

    public Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_events.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(
        DateTime start,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CalendarEvent> result = _events
            .Where(e => e.StartTime < endDate && e.EndTime > start)
            .OrderBy(e => e.StartTime)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(calendarEvent);
        return Task.FromResult(calendarEvent);
    }

    public Task UpdateAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        int index = _events.FindIndex(e => e.Id == calendarEvent.Id);
        if (index >= 0)
            _events[index] = calendarEvent;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _events.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }

    /// <summary>Directly seeds an event without going through the repository contract.</summary>
    public void Seed(CalendarEvent ev) => _events.Add(ev);

    /// <summary>Returns all stored events.</summary>
    public IReadOnlyList<CalendarEvent> All => _events.AsReadOnly();
}
