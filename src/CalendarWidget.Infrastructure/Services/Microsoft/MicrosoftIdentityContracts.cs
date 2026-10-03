namespace CalendarWidget.Infrastructure.Services.Outlook;

public sealed record MicrosoftOAuthOptions(string ClientId);

public sealed record MicrosoftAuthenticatedAccount(string AccountId, string DisplayName);

public interface IMicrosoftOAuthClient
{
    Task<MicrosoftAuthenticatedAccount> ConnectAsync(CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(string accountId, CancellationToken cancellationToken = default);
}

public interface IMicrosoftAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default);
}
