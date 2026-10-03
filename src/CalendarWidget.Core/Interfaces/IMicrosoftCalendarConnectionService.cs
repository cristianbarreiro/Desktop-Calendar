using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

public interface IMicrosoftCalendarConnectionService
{
    Task<CalendarAccount> ConnectMicrosoftAccountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarAccount>> GetMicrosoftAccountsAsync(CancellationToken cancellationToken = default);
    Task DisconnectMicrosoftAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Calendar>> DiscoverMicrosoftCalendarsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task SetMicrosoftCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default);
}
