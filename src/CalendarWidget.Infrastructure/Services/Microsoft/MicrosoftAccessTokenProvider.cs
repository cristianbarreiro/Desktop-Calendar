using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.Infrastructure.Services.Outlook;

public sealed class MicrosoftAccessTokenProvider(
    ICalendarCatalogRepository catalogs,
    IMicrosoftOAuthClient oauth) : IMicrosoftAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await catalogs.GetAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId && candidate.Provider == CalendarProvider.Microsoft)
            ?? throw new KeyNotFoundException("Connected Microsoft account was not found.");
        if (!account.IsConnected)
            throw new InvalidOperationException("Microsoft account is disconnected. Reconnect it before synchronizing.");
        return await oauth.GetAccessTokenAsync(account.ProviderAccountId, cancellationToken);
    }
}
