using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Services.Google;
using CalendarWidget.Infrastructure.Services.Outlook;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.Infrastructure.Services;

public sealed class ScopedCalendarConnectionService(IServiceScopeFactory scopeFactory) : ICalendarConnectionService
{
    public async Task<CalendarAccount> ConnectGoogleAccountAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<GoogleCalendarConnectionService>()
            .ConnectGoogleAccountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarAccount>> GetGoogleAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<GoogleCalendarConnectionService>()
            .GetGoogleAccountsAsync(cancellationToken);
    }

    public async Task DisconnectGoogleAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<GoogleCalendarConnectionService>()
            .DisconnectGoogleAccountAsync(accountId, cancellationToken);
    }

    public async Task<IReadOnlyList<Calendar>> DiscoverGoogleCalendarsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<GoogleCalendarConnectionService>()
            .DiscoverGoogleCalendarsAsync(accountId, cancellationToken);
    }

    public async Task SetCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<GoogleCalendarConnectionService>()
            .SetCalendarEnabledAsync(calendarId, enabled, cancellationToken);
    }
}

public sealed class ScopedMicrosoftCalendarConnectionService(IServiceScopeFactory scopeFactory)
    : IMicrosoftCalendarConnectionService
{
    public async Task<CalendarAccount> ConnectMicrosoftAccountAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MicrosoftCalendarConnectionService>()
            .ConnectMicrosoftAccountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarAccount>> GetMicrosoftAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MicrosoftCalendarConnectionService>()
            .GetMicrosoftAccountsAsync(cancellationToken);
    }

    public async Task DisconnectMicrosoftAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MicrosoftCalendarConnectionService>()
            .DisconnectMicrosoftAccountAsync(accountId, cancellationToken);
    }

    public async Task<IReadOnlyList<Calendar>> DiscoverMicrosoftCalendarsAsync(
        Guid accountId, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MicrosoftCalendarConnectionService>()
            .DiscoverMicrosoftCalendarsAsync(accountId, cancellationToken);
    }

    public async Task SetMicrosoftCalendarEnabledAsync(
        Guid calendarId, bool enabled, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MicrosoftCalendarConnectionService>()
            .SetMicrosoftCalendarEnabledAsync(calendarId, enabled, cancellationToken);
    }
}

public sealed class ScopedCalendarSynchronizationService(IServiceScopeFactory scopeFactory) : ICalendarSynchronizationService
{
    public async Task<CalendarSynchronizationBatchResult> SynchronizeEnabledCalendarsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CalendarAccount> accounts;
        IReadOnlyList<Calendar> calendars;
        await using (AsyncServiceScope discoveryScope = scopeFactory.CreateAsyncScope())
        {
            ICalendarCatalogRepository catalogs = discoveryScope.ServiceProvider
                .GetRequiredService<ICalendarCatalogRepository>();
            accounts = await catalogs.GetAccountsAsync(cancellationToken);
            calendars = await catalogs.GetCalendarsAsync(cancellationToken);
        }

        Dictionary<Guid, CalendarAccount> accountsById = accounts.ToDictionary(account => account.Id);
        List<CalendarSynchronizationCalendarResult> results = [];
        List<Calendar> eligibleCalendars = [];
        foreach (Calendar calendar in calendars.Where(item => item.IsEnabled && item.Provider != CalendarProvider.Local))
        {
            if (calendar.AccountId is not Guid accountId || !accountsById.TryGetValue(accountId, out CalendarAccount? account) ||
                account.Provider != calendar.Provider || !account.IsConnected)
            {
                results.Add(new CalendarSynchronizationCalendarResult(
                    calendar.Id, calendar.Provider, calendar.AccountId, Failure(
                        "Enabled calendar has no matching connected provider account.")));
                continue;
            }
            eligibleCalendars.Add(calendar);
        }

        foreach (Calendar calendar in eligibleCalendars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CalendarSynchronizationResult result;
            try
            {
                await using AsyncServiceScope calendarScope = scopeFactory.CreateAsyncScope();
                result = await calendarScope.ServiceProvider.GetRequiredService<CalendarSynchronizationService>()
                    .SynchronizeCalendarAsync(calendar.Id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = Failure(ex.Message);
            }
            results.Add(new CalendarSynchronizationCalendarResult(
                calendar.Id, calendar.Provider, calendar.AccountId, result));
        }
        return new CalendarSynchronizationBatchResult(results);
    }

    public async Task<CalendarSynchronizationResult> SynchronizeCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CalendarSynchronizationService>()
            .SynchronizeCalendarAsync(calendarId, cancellationToken);
    }

    public async Task<CalendarSynchronizationResult> DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CalendarSynchronizationService>()
            .DeleteEventAsync(eventId, cancellationToken);
    }

    private static CalendarSynchronizationResult Failure(string error)
    {
        CalendarSynchronizationFailure failure = new(null, null, error);
        return new CalendarSynchronizationResult(0, 0, 0, 0, 1, 0, null,
            [failure], [new CalendarSynchronizationItemResult(null, null, CalendarSynchronizationOutcome.Failed, error)]);
    }
}
