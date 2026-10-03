using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Persistence operations for provider accounts and calendars.
/// </summary>
public interface ICalendarCatalogRepository
{
    Task<IReadOnlyList<CalendarAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Calendar>> GetCalendarsAsync(CancellationToken cancellationToken = default);
    Task SaveAccountAsync(CalendarAccount account, CancellationToken cancellationToken = default);
    Task SaveCalendarAsync(Calendar calendar, CancellationToken cancellationToken = default);
}
