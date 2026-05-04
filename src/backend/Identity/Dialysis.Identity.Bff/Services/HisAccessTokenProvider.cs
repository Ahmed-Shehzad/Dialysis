using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Dialysis.Identity.Bff.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Dialysis.Identity.Bff.Services;

public interface IHisAccessTokenProvider
{
    Task<string?> GetAccessTokenForHisAsync(CancellationToken cancellationToken);
}

/// <summary>Exchanges the current user's Keycloak access token for an access token scoped to the HIS API audience.</summary>
public sealed class HisAccessTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakBffOptions> options,
    IMemoryCache cache,
    ILogger<HisAccessTokenProvider> logger) : IHisAccessTokenProvider
{
    private readonly KeycloakBffOptions _options = options.Value;

    public async Task<string?> GetAccessTokenForHisAsync(CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated != true)
            return null;

        var subjectToken = await http.GetTokenAsync("access_token").ConfigureAwait(false);
        if (string.IsNullOrEmpty(subjectToken))
            return null;

        var sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? http.User.FindFirst("sub")?.Value
            ?? "anonymous";
        var cacheKey = $"his-exchanged:{sub}:{subjectToken.GetHashCode(StringComparison.Ordinal)}";
        if (cache.TryGetValue(cacheKey, out string? hit) && !string.IsNullOrEmpty(hit))
            return hit;

        var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/protocol/openid-connect/token";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["subject_token"] = subjectToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["audience"] = _options.HisAudienceClientId,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var client = httpClientFactory.CreateClient("keycloak");
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Token exchange failed {Status}: {Body}", (int)response.StatusCode, err);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("access_token", out var atEl))
            return null;
        var exchanged = atEl.GetString();
        if (string.IsNullOrEmpty(exchanged))
            return null;

        var ttl = TimeSpan.FromMinutes(4);
        if (doc.RootElement.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var sec) && sec > 60)
            ttl = TimeSpan.FromSeconds(Math.Min(sec - 30, 600));

        cache.Set(cacheKey, exchanged, ttl);
        return exchanged;
    }
}
