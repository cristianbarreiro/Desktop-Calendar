using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace CalendarWidget.Infrastructure.Services.Outlook;

public sealed class MicrosoftOAuthClient : IMicrosoftOAuthClient
{
    private static readonly string[] Scopes = ["User.Read", "Calendars.ReadWrite"];
    private readonly MicrosoftOAuthOptions _options;
    private readonly Lazy<Task<IPublicClientApplication>> _application;

    public MicrosoftOAuthClient(MicrosoftOAuthOptions options)
    {
        _options = options;
        _application = new Lazy<Task<IPublicClientApplication>>(
            CreateApplicationAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<MicrosoftAuthenticatedAccount> ConnectAsync(CancellationToken cancellationToken = default)
    {
        IPublicClientApplication application = await _application.Value;
        AuthenticationResult result = await application.AcquireTokenInteractive(Scopes)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync(cancellationToken);
        IAccount account = result.Account ?? throw new InvalidDataException("Microsoft sign-in did not return an account identity.");
        string accountId = account.HomeAccountId?.Identifier
            ?? throw new InvalidDataException("Microsoft sign-in did not return a stable account identity.");
        return new MicrosoftAuthenticatedAccount(accountId, account.Username ?? accountId);
    }

    public async Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        IPublicClientApplication application = await _application.Value;
        IAccount? account = (await application.GetAccountsAsync())
            .SingleOrDefault(candidate => candidate.HomeAccountId?.Identifier == accountId);
        if (account is null)
            throw new InvalidOperationException("Microsoft sign-in has expired. Reconnect the Microsoft account.");

        try
        {
            AuthenticationResult result = await application.AcquireTokenSilent(Scopes, account)
                .ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException ex)
        {
            throw new InvalidOperationException("Microsoft sign-in requires user interaction. Reconnect the Microsoft account.", ex);
        }
    }

    public async Task DisconnectAsync(string accountId, CancellationToken cancellationToken = default)
    {
        IPublicClientApplication application = await _application.Value;
        IAccount? account = (await application.GetAccountsAsync())
            .SingleOrDefault(candidate => candidate.HomeAccountId?.Identifier == accountId);
        if (account is not null)
            await application.RemoveAsync(account);
    }

    private async Task<IPublicClientApplication> CreateApplicationAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Set DESKTOP_CALENDAR_MICROSOFT_CLIENT_ID to the public-client application ID.");

        string cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopCalendar", "MicrosoftAuth");
        Directory.CreateDirectory(cacheDirectory);
        IPublicClientApplication application = PublicClientApplicationBuilder.Create(_options.ClientId)
            .WithAuthority("https://login.microsoftonline.com/common")
            .WithDefaultRedirectUri()
            .Build();
        StorageCreationProperties storage = new StorageCreationPropertiesBuilder("msal-cache.bin", cacheDirectory).Build();
        MsalCacheHelper cache = await MsalCacheHelper.CreateAsync(storage);
        cache.RegisterCache(application.UserTokenCache);
        return application;
    }
}
