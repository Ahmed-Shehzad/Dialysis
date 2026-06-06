using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Dialysis.DataSimulator;

/// <summary>Supplies a service-account bearer token for the module calls.</summary>
public interface IAccessTokenProvider
{
    /// <summary>Gets a (cached) client-credentials access token.</summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Mints + caches a Keycloak client-credentials token. The token carries the <c>dialysis_permission</c>
/// claim (via the realm's hardcoded mapper) so the module APIs authorize the write calls.
/// </summary>
public sealed class ClientCredentialsTokenProvider : IAccessTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAtUtc;

    /// <summary>Creates the provider.</summary>
    public ClientCredentialsTokenProvider(IHttpClientFactory httpClientFactory, IOptions<DataSimulatorOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value.Auth;
    }

    /// <inheritdoc />
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAtUtc)
            return _token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAtUtc)
                return _token;

            if (string.IsNullOrWhiteSpace(_options.Authority))
                throw new InvalidOperationException("DataSimulator:Auth:Authority is required.");

            var tokenEndpoint = _options.Authority.TrimEnd('/') + _options.TokenPath;
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret ?? string.Empty,
                }),
            };

            using var client = _httpClientFactory.CreateClient("token");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = doc.RootElement;
            _token = root.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("Token endpoint returned no access_token.");
            var expiresIn = root.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 60;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>Attaches the service-account bearer token to every outbound module request.</summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;

    /// <summary>Creates the handler.</summary>
    public BearerTokenHandler(IAccessTokenProvider tokenProvider) => _tokenProvider = tokenProvider;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
