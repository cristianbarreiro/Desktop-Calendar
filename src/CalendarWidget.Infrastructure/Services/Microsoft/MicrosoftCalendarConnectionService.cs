using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Infrastructure.Services.Outlook;

public sealed class MicrosoftCalendarConnectionService(
    IMicrosoftOAuthClient oauth,
    ICalendarCatalogRepository catalogs,
    ICalendarProviderRegistry providers) : IMicrosoftCalendarConnectionService
{
    public async Task<CalendarAccount> ConnectMicrosoftAccountAsync(CancellationToken cancellationToken = default)
    {
        MicrosoftAuthenticatedAccount identity = await oauth.ConnectAsync(cancellationToken);
        IReadOnlyList<CalendarAccount> accounts = await catalogs.GetAccountsAsync(cancellationToken);
        CalendarAccount? account = accounts.SingleOrDefault(candidate =>
            candidate.Provider == CalendarProvider.Microsoft && candidate.ProviderAccountId == identity.AccountId);
        account ??= new CalendarAccount
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = identity.AccountId,
            DisplayName = identity.DisplayName,
            CreatedAt = DateTime.UtcNow,
        };
        account.DisplayName = identity.DisplayName;
        account.IsConnected = true;
        await catalogs.SaveAccountAsync(account, cancellationToken);
        await DiscoverMicrosoftCalendarsAsync(account.Id, cancellationToken);
        return account;
    }

    public async Task<IReadOnlyList<CalendarAccount>> GetMicrosoftAccountsAsync(CancellationToken cancellationToken = default) =>
        (await catalogs.GetAccountsAsync(cancellationToken))
        .Where(account => account.Provider == CalendarProvider.Microsoft)
        .ToArray();

    public async Task DisconnectMicrosoftAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await GetMicrosoftAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId)
            ?? throw new DomainValidationException("Microsoft account was not found.");
        account.IsConnected = false;
        await catalogs.SaveAccountAsync(account, cancellationToken);
        foreach (Calendar calendar in (await catalogs.GetCalendarsAsync(cancellationToken))
                     .Where(calendar => calendar.AccountId == accountId && calendar.Provider == CalendarProvider.Microsoft))
        {
            calendar.IsEnabled = false;
            await catalogs.SaveCalendarAsync(calendar, cancellationToken);
        }
        await oauth.DisconnectAsync(account.ProviderAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<Calendar>> DiscoverMicrosoftCalendarsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await GetMicrosoftAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId)
            ?? throw new DomainValidationException("Microsoft account was not found.");
        if (!account.IsConnected)
            return (await catalogs.GetCalendarsAsync(cancellationToken))
                .Where(calendar => calendar.Provider == CalendarProvider.Microsoft && calendar.AccountId == accountId)
                .ToArray();
        IReadOnlyList<CalendarProviderCalendar> discovered = await providers.GetProvider(CalendarProvider.Microsoft)
            .GetCalendarsAsync(accountId, cancellationToken);
        IReadOnlyList<Calendar> existing = await catalogs.GetCalendarsAsync(cancellationToken);
        HashSet<string> discoveredIds = discovered.Select(item => item.ExternalCalendarId).ToHashSet(StringComparer.Ordinal);
        foreach (Calendar previous in existing.Where(candidate => candidate.Provider == CalendarProvider.Microsoft &&
                     candidate.AccountId == account.Id && candidate.IsEnabled && !discoveredIds.Contains(candidate.ExternalId)))
        {
            previous.IsEnabled = false;
            await catalogs.SaveCalendarAsync(previous, cancellationToken);
        }

        List<Calendar> results = [];
        foreach (CalendarProviderCalendar item in discovered)
        {
            Calendar? calendar = existing.SingleOrDefault(candidate => candidate.Provider == CalendarProvider.Microsoft &&
                candidate.AccountId == account.Id && candidate.ExternalId == item.ExternalCalendarId);
            calendar ??= new Calendar
            {
                Id = Guid.NewGuid(),
                Provider = CalendarProvider.Microsoft,
                AccountId = account.Id,
                Name = item.Name,
                ExternalId = item.ExternalCalendarId,
                IsEnabled = false,
                CreatedAt = DateTime.UtcNow,
            };
            calendar.Name = item.Name;
            await catalogs.SaveCalendarAsync(calendar, cancellationToken);
            results.Add(calendar);
        }
        return results;
    }

    public async Task SetMicrosoftCalendarEnabledAsync(
        Guid calendarId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        Calendar calendar = (await catalogs.GetCalendarsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == calendarId && candidate.Provider == CalendarProvider.Microsoft)
            ?? throw new DomainValidationException("Microsoft calendar was not found.");
        CalendarAccount account = (await GetMicrosoftAccountsAsync(cancellationToken))
            .Single(candidate => candidate.Id == calendar.AccountId);
        if (enabled && !account.IsConnected)
            throw new InvalidOperationException("Reconnect the Microsoft account before enabling its calendars.");
        calendar.IsEnabled = enabled;
        await catalogs.SaveCalendarAsync(calendar, cancellationToken);
    }
}
