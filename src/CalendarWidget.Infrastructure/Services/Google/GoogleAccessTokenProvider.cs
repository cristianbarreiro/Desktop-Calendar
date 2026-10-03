using System.Text.Json;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.Infrastructure.Services.Google;

public sealed class GoogleAccessTokenProvider(
    ICalendarCatalogRepository catalogs,
    IGoogleCredentialStore credentialStore,
    IGoogleOAuthClient oauth) : IGoogleAccessTokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        CalendarAccount account = (await catalogs.GetAccountsAsync(cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == accountId && candidate.Provider == CalendarProvider.Google)
            ?? throw new KeyNotFoundException("Connected Google account was not found.");
        if (!account.IsConnected)
            throw new InvalidOperationException("Google account is disconnected. Reconnect it before synchronizing.");
        string? serialized = await credentialStore.ReadAsync(account.ProviderAccountId, cancellationToken);
        StoredGoogleCredential credential = serialized is null
            ? throw new InvalidOperationException("Google account credentials are unavailable. Reconnect the account.")
            : JsonSerializer.Deserialize<StoredGoogleCredential>(serialized, JsonOptions)
              ?? throw new InvalidDataException("Stored Google credential is invalid.");

        if (credential.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            return credential.AccessToken;

        GoogleOAuthTokenSet refreshed = await oauth.RefreshAsync(credential.RefreshToken, cancellationToken);
        StoredGoogleCredential updated = new(refreshed.AccessToken, refreshed.RefreshToken ?? credential.RefreshToken, refreshed.ExpiresAtUtc);
        await credentialStore.WriteAsync(account.ProviderAccountId, JsonSerializer.Serialize(updated, JsonOptions), cancellationToken);
        return updated.AccessToken;
    }
}
