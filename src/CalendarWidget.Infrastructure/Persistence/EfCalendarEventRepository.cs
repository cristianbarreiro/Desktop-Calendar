using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.Infrastructure.Persistence;

/// <summary>
/// EF Core SQLite implementation of <see cref="ICalendarEventRepository"/>.
/// </summary>
public sealed class EfCalendarEventRepository(AppDbContext context) : ICalendarEventRepository
{
    /// <inheritdoc />
    public async Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.CalendarEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(
        DateTime start,
        DateTime endDate,
        CancellationToken cancellationToken = default)
        => await context.CalendarEvents
            .AsNoTracking()
            .Where(e => e.StartTime < endDate && e.EndTime > start)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        context.CalendarEvents.Add(calendarEvent);
        await context.SaveChangesAsync(cancellationToken);
        return calendarEvent;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        context.CalendarEvents.Update(calendarEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CalendarEvent? entity = await context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
            return;

        context.CalendarEvents.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
