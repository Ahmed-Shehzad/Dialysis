using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;
using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// OAuth 2.0 client-credentials grant (RFC 6749 §4.4). POSTs <c>grant_type=client_credentials</c> to
/// the configured token endpoint, parses <c>access_token</c> + <c>expires_in</c>, caches the token
/// in <see cref="IDistributedCache"/> (Valkey in production, in-memory in dev) so multiple SmartConnect
/// replicas share one token, then attaches it as <c>Authorization: Bearer …</c> on every outbound send.
/// </summary>
/// <remarks>
/// Use this for partner integrations where SmartConnect itself is the client (machine-to-machine):
/// Epic backend services, Cerner system accounts, lab portals exposing OAuth2 token endpoints, etc.
/// For vendor-specific flows that need JWT-signed assertions, the existing
/// <c>OAuth2TokenAcquirer</c> in <c>Adapters.Common</c> remains the right tool.
/// </remarks>
public sealed class OAuth2ClientCredentialsAuthenticationProvider(
    IHttpClientFactory httpClientFactory,
    IDistributedCache tokenCache) : IHttpAuthenticationProvider
{
    private static readonly TimeSpan ExpirationSkew = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinimumTtl = TimeSpan.FromSeconds(5);

    public string Kind => "oauth2-client-credentials";

    public async Task ApplyAsync(HttpRequestMessage request, string parametersJson, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = JsonSerializer.Deserialize<OAuth2Options>(parametersJson)
            ?? throw new InvalidOperationException("OAuth2 authentication parameters must be a JSON object.");
        if (string.IsNullOrWhiteSpace(options.TokenEndpoint))
            throw new InvalidOperationException("OAuth2 authentication parameters must include 'TokenEndpoint'.");
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("OAuth2 authentication parameters must include 'ClientId'.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            throw new InvalidOperationException("OAuth2 authentication parameters must include 'ClientSecret'.");

        var cacheKey = BuildCacheKey(options);
        var cached = await tokenCache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        string accessToken;
        if (cached is { Length: > 0 })
        {
            accessToken = Encoding.UTF8.GetString(cached);
        }
        else
        {
            accessToken = await FetchTokenAsync(options, cacheKey, cancellationToken).ConfigureAwait(false);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<string> FetchTokenAsync(OAuth2Options options, string cacheKey, CancellationToken cancellationToken)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", options.ClientId!),
            new("client_secret", options.ClientSecret!),
        };
        if (!string.IsNullOrWhiteSpace(options.Scope))
            fields.Add(new KeyValuePair<string, string>("scope", options.Scope));

        using var form = new FormUrlEncodedContent(fields);
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint) { Content = form };
        var client = httpClientFactory.CreateClient("smartconnect-outbound");
        using var response = await client.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"OAuth2 token endpoint {options.TokenEndpoint} returned {(int)response.StatusCode}: {error}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("OAuth2 token response missing 'access_token'.");
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var seconds)
            ? seconds
            : 300;

        var ttl = TimeSpan.FromSeconds(expiresIn) - ExpirationSkew;
        if (ttl < MinimumTtl)
            ttl = MinimumTtl;
        await tokenCache.SetAsync(
            cacheKey,
            Encoding.UTF8.GetBytes(accessToken),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken).ConfigureAwait(false);
        return accessToken;
    }

    private static string BuildCacheKey(OAuth2Options options) =>
        $"smartconnect:oauth2:{options.TokenEndpoint}:{options.ClientId}:{options.Scope ?? "_"}";

    private sealed class OAuth2Options
    {
        public string? TokenEndpoint { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }

        public string? Scope { get; set; }
    }
}
