using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.SmartConnect.Adapters;

/// <summary>
/// Shared helper for vendor adapters: POSTs a form to the OAuth2 token endpoint, parses the
/// <c>access_token</c> + <c>expires_in</c>, and caches per-tenant in <see cref="IDistributedCache"/>
/// (Valkey / Redis in production, in-memory in dev) so multiple replicas share one token. Vendor-
/// specific concerns (grant type, auth header, JWT signing) are supplied via <see cref="OAuth2TokenRequest"/>.
/// </summary>
public sealed class OAuth2TokenAcquirer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _tokenCache;
    private readonly TimeSpan _skew;

    public OAuth2TokenAcquirer(IHttpClientFactory httpClientFactory, IDistributedCache tokenCache, TimeSpan? expirationSkew = null)
    {
        _httpClientFactory = httpClientFactory;
        _tokenCache = tokenCache;
        _skew = expirationSkew ?? TimeSpan.FromSeconds(30);
    }

    public async Task<string> AcquireAsync(OAuth2TokenRequest request, CancellationToken cancellationToken)
    {
        var cachedBytes = await _tokenCache.GetAsync(request.CacheKey, cancellationToken).ConfigureAwait(false);
        if (cachedBytes is not null && cachedBytes.Length > 0)
        {
            return Encoding.UTF8.GetString(cachedBytes);
        }

        using var form = new FormUrlEncodedContent(request.FormFields);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.TokenEndpoint);
        httpRequest.Content = form;
        if (request.BasicAuth is { } basic)
        {
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basic.User}:{basic.Password}"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }
        if (request.ExtraHeaders is not null)
        {
            foreach (var (key, value) in request.ExtraHeaders)
                httpRequest.Headers.TryAddWithoutValidation(key, value);
        }

        using var client = _httpClientFactory.CreateClient(request.VendorName);
        using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"{request.VendorName} token endpoint returned {(int)response.StatusCode}: {error}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException($"{request.VendorName} token response missing access_token.");
        var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var s) ? s : 300;

        var ttl = TimeSpan.FromSeconds(expiresInSeconds) - _skew;
        if (ttl < TimeSpan.FromSeconds(5))
            ttl = TimeSpan.FromSeconds(5);
        await _tokenCache.SetAsync(
            request.CacheKey,
            Encoding.UTF8.GetBytes(accessToken),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken).ConfigureAwait(false);
        return accessToken;
    }
}

public sealed record OAuth2TokenRequest(
    string VendorName,
    string TokenEndpoint,
    string CacheKey,
    IReadOnlyList<KeyValuePair<string, string>> FormFields,
    BasicAuthCredential? BasicAuth = null,
    IReadOnlyDictionary<string, string>? ExtraHeaders = null);

public sealed record BasicAuthCredential(string User, string Password);
