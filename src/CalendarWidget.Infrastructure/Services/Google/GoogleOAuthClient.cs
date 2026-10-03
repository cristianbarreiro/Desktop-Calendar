using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CalendarWidget.Infrastructure.Services.Google;

public sealed class GoogleOAuthOptions(string clientId)
{
    public string ClientId { get; } = clientId;
}

/// <summary>Implements Google's installed desktop-app OAuth authorization-code flow with PKCE.</summary>
public sealed class GoogleOAuthClient(HttpClient httpClient, GoogleOAuthOptions options) : IGoogleOAuthClient
{
    private static readonly string[] Scopes =
    [
        "openid",
        "email",
        "https://www.googleapis.com/auth/calendar.readonly",
    ];

    public async Task<(GoogleAuthenticatedUser User, GoogleOAuthTokenSet Tokens)> AuthorizeAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("Configure DESKTOP_CALENDAR_GOOGLE_CLIENT_ID with a Google OAuth Desktop client ID before connecting an account.");

        string verifier = CreateVerifier();
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        string state = Base64Url(RandomNumberGenerator.GetBytes(32));
        int port = GetAvailableLoopbackPort();
        string redirectUri = $"http://127.0.0.1:{port}/";
        using HttpListener listener = new();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        string authorizationUrl = BuildAuthorizationUrl(options.ClientId, redirectUri, challenge, state);
        Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true });

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        HttpListenerContext callback = await listener.GetContextAsync().WaitAsync(timeout.Token);
        string? error = QueryValue(callback.Request.Url, "error");
        string? returnedState = QueryValue(callback.Request.Url, "state");
        string? code = QueryValue(callback.Request.Url, "code");
        bool validResponse = error is null && code is not null && returnedState is not null && FixedTimeEquals(state, returnedState);
        await WriteCallbackResponseAsync(callback.Response, validResponse);

        if (error is not null)
            throw new InvalidOperationException("Google authorization was not completed.");
        if (!validResponse || code is null)
            throw new InvalidOperationException("Google authorization response failed state validation.");

        GoogleOAuthTokenSet tokens = await ExchangeCodeAsync(code, verifier, redirectUri, cancellationToken);
        GoogleAuthenticatedUser user = await GetUserAsync(tokens.AccessToken, cancellationToken);
        return (user, tokens);
    }

    public async Task<GoogleOAuthTokenSet> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> parameters = new()
        {
            ["client_id"] = options.ClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };
        return await RequestTokensAsync(parameters, refreshToken, cancellationToken);
    }

    private async Task<GoogleOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string verifier,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> parameters = new()
        {
            ["client_id"] = options.ClientId,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        };
        return await RequestTokensAsync(parameters, null, cancellationToken);
    }

    private async Task<GoogleOAuthTokenSet> RequestTokensAsync(
        Dictionary<string, string> parameters,
        string? existingRefreshToken,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(parameters),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        JsonElement root = document.RootElement;
        string accessToken = RequiredString(root, "access_token");
        string? refreshToken = root.TryGetProperty("refresh_token", out JsonElement refreshElement)
            ? refreshElement.GetString()
            : existingRefreshToken;
        int expiresIn = root.TryGetProperty("expires_in", out JsonElement expiresElement)
            ? expiresElement.GetInt32()
            : 3600;
        return new GoogleOAuthTokenSet(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
    }

    private async Task<GoogleAuthenticatedUser> GetUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        JsonElement root = document.RootElement;
        return new GoogleAuthenticatedUser(
            RequiredString(root, "sub"),
            RequiredString(root, "email"),
            root.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? root.GetProperty("email").GetString()! : root.GetProperty("email").GetString()!);
    }

    private static string BuildAuthorizationUrl(string clientId, string redirectUri, string challenge, string state)
    {
        Dictionary<string, string> parameters = new()
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', Scopes),
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
        };
        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join(
            "&", parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static async Task WriteCallbackResponseAsync(HttpListenerResponse response, bool success)
    {
        response.ContentType = "text/html; charset=utf-8";
        byte[] body = Encoding.UTF8.GetBytes(success
            ? "<html><body>Google Calendar connected. You can return to Desktop Calendar.</body></html>"
            : "<html><body>Google authorization was not completed. You can close this window.</body></html>");
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body);
        response.Close();
    }

    private static int GetAvailableLoopbackPort()
    {
        using System.Net.Sockets.TcpListener socket = new(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        return ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
    }

    private static string CreateVerifier() => Base64Url(RandomNumberGenerator.GetBytes(64));

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string? QueryValue(Uri? uri, string key)
    {
        if (uri is null)
            return null;
        foreach (string field in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = field.Split('=', 2);
            if (Uri.UnescapeDataString(pair[0].Replace('+', ' ')) == key)
                return pair.Length == 2 ? Uri.UnescapeDataString(pair[1].Replace('+', ' ')) : string.Empty;
        }
        return null;
    }

    private static string RequiredString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidDataException($"Google OAuth response did not include '{name}'.");
}
