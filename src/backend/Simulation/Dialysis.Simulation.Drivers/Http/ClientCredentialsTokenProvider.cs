using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Supplies a bearer token for the driver calls.</summary>
public interface IClientCredentialsTokenProvider
{
    /// <summary>Gets a (cached) access token for the configured client.</summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Mints + caches a client-credentials access token from Keycloak. The token carries the
/// <c>dialysis_permission</c> claim (via the realm's hardcoded mapper) so the driver calls satisfy each
/// module's authorization.
/// </summary>
public sealed class KeycloakClientCredentialsTokenProvider : IClientCredentialsTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SimulationDriverOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAtUtc;

    /// <summary>Creates the provider.</summary>
    public KeycloakClientCredentialsTokenProvider(IHttpClientFactory httpClientFactory, IOptions<SimulationDriverOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
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
                throw new InvalidOperationException("Simulation:Drivers:Authority is required for HTTP driver mode.");

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

            using var client = _httpClientFactory.CreateClient("simulation-token");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = doc.RootElement;
            var token = root.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("Token endpoint returned no access_token.");
            var expiresIn = root.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 60;

            _token = token;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }
}
