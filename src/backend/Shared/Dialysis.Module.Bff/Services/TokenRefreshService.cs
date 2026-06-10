using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dialysis.Module.Bff.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Module.Bff.Services;

/// <summary>
/// Refreshes the Keycloak access/refresh-token pair stashed on the BFF cookie ticket. Wired to the
/// cookie handler's <c>OnValidatePrincipal</c> so every authenticated request near expiry rolls
/// forward — without this the short-lived access token expires while the SPA still holds a valid
/// BFF cookie, and proxied <c>{base}/api/*</c> calls start 401ing until the user re-signs in.
/// </summary>
public interface ITokenRefreshService
{
    Task ValidateAsync(CookieValidatePrincipalContext context);
}

public sealed class TokenRefreshService : ITokenRefreshService
{
    private readonly KeycloakBffOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<TokenRefreshService> _logger;
    public TokenRefreshService(IHttpClientFactory httpClientFactory,
        IOptions<KeycloakBffOptions> options,
        TimeProvider clock,
        ILogger<TokenRefreshService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _clock = clock;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>Window before <c>expires_at</c> in which we proactively refresh.</summary>
    private static readonly TimeSpan _refreshSkew = TimeSpan.FromSeconds(60);

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var properties = context.Properties;
        var expiresAtRaw = properties.GetTokenValue("expires_at");
        if (string.IsNullOrWhiteSpace(expiresAtRaw)
            || !DateTimeOffset.TryParse(
                expiresAtRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            return;
        }

        var now = _clock.GetUtcNow();
        if (expiresAt - now > _refreshSkew)
        {
            return;
        }

        var refreshToken = properties.GetTokenValue("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogInformation("BFF session has no refresh_token; rejecting principal to force re-login.");
            await RejectAsync(context).ConfigureAwait(false);
            return;
        }

        var refreshed = await CallKeycloakAsync(refreshToken, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (refreshed is null)
        {
            _logger.LogInformation("Token refresh failed; rejecting principal so the SPA re-authenticates.");
            await RejectAsync(context).ConfigureAwait(false);
            return;
        }

        UpdateTokens(properties, refreshed, now);
        context.ShouldRenew = true;
    }

    private async Task<RefreshedTokens?> CallKeycloakAsync(string refreshToken, CancellationToken ct)
    {
        var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/protocol/openid-connect/token";
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            }),
        };

        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var client = _httpClientFactory.CreateClient("keycloak");
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Refresh-token grant failed {Status}: {Body}", (int)response.StatusCode, body);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;
        var accessToken = root.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
        if (string.IsNullOrEmpty(accessToken))
            return null;

        var newRefresh = root.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : null;
        var idToken = root.TryGetProperty("id_token", out var idEl) ? idEl.GetString() : null;
        var expiresInSec = root.TryGetProperty("expires_in", out var exEl) && exEl.TryGetInt32(out var sec) ? sec : 300;
        return new RefreshedTokens(accessToken, newRefresh, idToken, expiresInSec);
    }

    private static void UpdateTokens(AuthenticationProperties properties, RefreshedTokens refreshed, DateTimeOffset now)
    {
        properties.UpdateTokenValue("access_token", refreshed.AccessToken);
        if (!string.IsNullOrEmpty(refreshed.RefreshToken))
            properties.UpdateTokenValue("refresh_token", refreshed.RefreshToken);
        if (!string.IsNullOrEmpty(refreshed.IdToken))
            properties.UpdateTokenValue("id_token", refreshed.IdToken);
        var newExpiry = now.AddSeconds(refreshed.ExpiresInSeconds);
        properties.UpdateTokenValue("expires_at", newExpiry.ToString("o", CultureInfo.InvariantCulture));
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        try
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // No sign-out handler registered (unit-test harness) — RejectPrincipal already did the work.
        }
    }

    private sealed record RefreshedTokens
    {
        public RefreshedTokens(string AccessToken, string? RefreshToken, string? IdToken, int ExpiresInSeconds)
        {
            this.AccessToken = AccessToken;
            this.RefreshToken = RefreshToken;
            this.IdToken = IdToken;
            this.ExpiresInSeconds = ExpiresInSeconds;
        }
        public string AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public string? IdToken { get; init; }
        public int ExpiresInSeconds { get; init; }
        public void Deconstruct(out string accessToken, out string? refreshToken, out string? idToken, out int expiresInSeconds)
        {
            accessToken = AccessToken;
            refreshToken = RefreshToken;
            idToken = IdToken;
            expiresInSeconds = ExpiresInSeconds;
        }
    }
}
