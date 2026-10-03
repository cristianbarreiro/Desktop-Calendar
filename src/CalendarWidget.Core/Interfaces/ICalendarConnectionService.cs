using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

public interface ICalendarConnectionService
{
    Task<CalendarAccount> ConnectGoogleAccountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarAccount>> GetGoogleAccountsAsync(CancellationToken cancellationToken = default);
    Task DisconnectGoogleAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Calendar>> DiscoverGoogleCalendarsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task SetCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default);
}
