namespace CalendarWidget.Infrastructure.Services.Google;

public sealed record GoogleOAuthTokenSet(string AccessToken, string? RefreshToken, DateTime ExpiresAtUtc);

public sealed record GoogleAuthenticatedUser(string Subject, string Email, string DisplayName);

public sealed record StoredGoogleCredential(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

public interface IGoogleCredentialStore
{
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default);
    Task WriteAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public interface IGoogleOAuthClient
{
    Task<(GoogleAuthenticatedUser User, GoogleOAuthTokenSet Tokens)> AuthorizeAsync(CancellationToken cancellationToken = default);
    Task<GoogleOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public interface IGoogleAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default);
}
