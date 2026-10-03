using System.Text.Json;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Infrastructure.Services.Google;

public sealed class GoogleCalendarConnectionService(
    IGoogleOAuthClient oauth,
    IGoogleCredentialStore credentials,
    ICalendarCatalogRepository catalogs,
    ICalendarProviderRegistry providers) : ICalendarConnectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CalendarAccount> ConnectGoogleAccountAsync(CancellationToken cancellationToken = default)
    {
        (GoogleAuthenticatedUser user, GoogleOAuthTokenSet tokens) = await oauth.AuthorizeAsync(cancellationToken);
        IReadOnlyList<CalendarAccount> accounts = await catalogs.GetAccountsAsync(cancellationToken);
        CalendarAccount? account = accounts.SingleOrDefault(candidate =>
            candidate.Provider == CalendarProvider.Google && candidate.ProviderAccountId == user.Subject);
        account ??= new CalendarAccount
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = user.Subject,
            DisplayName = user.Email,
            CreatedAt = DateTime.UtcNow,
        };
        account.DisplayName = user.Email;
        account.IsConnected = true;

        string? previous = await credentials.ReadAsync(account.ProviderAccountId, cancellationToken);
        string refreshToken = tokens.RefreshToken
            ?? (previous is null
                ? throw new InvalidOperationException("Google did not return a refresh token. Reconnect and grant offline access.")
                : (JsonSerializer.Deserialize<StoredGoogleCredential>(previous, JsonOptions)
                   ?? throw new InvalidDataException("Stored Google credentials are invalid.")).RefreshToken);
        await credentials.WriteAsync(account.ProviderAccountId, JsonSerializer.Serialize(
            new StoredGoogleCredential(tokens.AccessToken, refreshToken, tokens.ExpiresAtUtc), JsonOptions), cancellationToken);
        await catalogs.SaveAccountAsync(account, cancellationToken);
        await DiscoverGoogleCalendarsAsync(account.Id, cancellationToken);
        return account;
    }

    public async Task<IReadOnlyList<CalendarAccount>> GetGoogleAccountsAsync(CancellationToken cancellationToken = default) =>
        (await catalogs.GetAccountsAsync(cancellationToken))
        .Where(account => account.Provider == CalendarProvider.Google)
        .ToArray();

    public async Task DisconnectGoogleAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await GetGoogleAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId)
            ?? throw new DomainValidationException("Google account was not found.");
        account.IsConnected = false;
        await catalogs.SaveAccountAsync(account, cancellationToken);
        foreach (Calendar calendar in (await catalogs.GetCalendarsAsync(cancellationToken))
                     .Where(calendar => calendar.AccountId == accountId && calendar.Provider == CalendarProvider.Google))
        {
            calendar.IsEnabled = false;
            await catalogs.SaveCalendarAsync(calendar, cancellationToken);
        }
        await credentials.DeleteAsync(account.ProviderAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<Calendar>> DiscoverGoogleCalendarsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await GetGoogleAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId)
            ?? throw new DomainValidationException("Google account was not found.");
        if (!account.IsConnected)
            return (await catalogs.GetCalendarsAsync(cancellationToken))
                .Where(calendar => calendar.Provider == CalendarProvider.Google && calendar.AccountId == accountId)
                .ToArray();
        ICalendarProvider google = providers.GetProvider(CalendarProvider.Google);
        IReadOnlyList<CalendarProviderCalendar> discovered = await google.GetCalendarsAsync(accountId, cancellationToken);
        IReadOnlyList<Calendar> existing = await catalogs.GetCalendarsAsync(cancellationToken);
        List<Calendar> results = [];
        HashSet<string> discoveredIds = discovered.Select(item => item.ExternalCalendarId).ToHashSet(StringComparer.Ordinal);
        foreach (Calendar previouslyDiscovered in existing.Where(candidate =>
                     candidate.Provider == CalendarProvider.Google && candidate.AccountId == account.Id &&
                     !discoveredIds.Contains(candidate.ExternalId) && candidate.IsEnabled))
        {
            previouslyDiscovered.IsEnabled = false;
            await catalogs.SaveCalendarAsync(previouslyDiscovered, cancellationToken);
        }
        foreach (CalendarProviderCalendar item in discovered)
        {
            Calendar? calendar = existing.SingleOrDefault(candidate =>
                candidate.Provider == CalendarProvider.Google && candidate.AccountId == account.Id &&
                candidate.ExternalId == item.ExternalCalendarId);
            calendar ??= new Calendar
            {
                Id = Guid.NewGuid(),
                Provider = CalendarProvider.Google,
                AccountId = account.Id,
                Name = item.Name,
                ExternalId = item.ExternalCalendarId,
                CreatedAt = DateTime.UtcNow,
                IsEnabled = false,
            };
            calendar.Name = item.Name;
            await catalogs.SaveCalendarAsync(calendar, cancellationToken);
            results.Add(calendar);
        }
        return results;
    }

    public async Task SetCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default)
    {
        Calendar calendar = (await catalogs.GetCalendarsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == calendarId && candidate.Provider == CalendarProvider.Google)
            ?? throw new DomainValidationException("Google calendar was not found.");
        CalendarAccount account = (await GetGoogleAccountsAsync(cancellationToken))
            .Single(candidate => candidate.Id == calendar.AccountId);
        if (enabled && !account.IsConnected)
            throw new InvalidOperationException("Reconnect the Google account before enabling its calendars.");
        calendar.IsEnabled = enabled;
        await catalogs.SaveCalendarAsync(calendar, cancellationToken);
    }
}
