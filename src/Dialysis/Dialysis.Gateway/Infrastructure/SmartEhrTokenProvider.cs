using System.Text.Json;

using Dialysis.SharedKernel.Abstractions;

using Duende.IdentityModel.Client;

using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// SMART on FHIR: obtains access tokens via OAuth2 client credentials for PDMS → EHR calls.
/// Discovers token endpoint from /.well-known/smart-configuration when TokenEndpoint is not set.
/// </summary>
public sealed class SmartEhrTokenProvider : ISmartFhirTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly EhrOutboundOptions _options;
    private readonly ILogger<SmartEhrTokenProvider> _logger;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public SmartEhrTokenProvider(
        HttpClient httpClient,
        IOptions<EhrOutboundOptions> options,
        ILogger<SmartEhrTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.FhirBaseUrl) &&
        !string.IsNullOrWhiteSpace(_options.ClientId) &&
        !string.IsNullOrWhiteSpace(_options.ClientSecret);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-2))
            return _cachedToken;

        var tokenUrl = _options.TokenEndpoint;
        if (string.IsNullOrWhiteSpace(tokenUrl))
        {
            tokenUrl = await DiscoverTokenEndpointAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(tokenUrl))
            {
                _logger.LogWarning("SMART: Could not discover token endpoint from {BaseUrl}", _options.FhirBaseUrl);
                return null;
            }
        }

        var request = new ClientCredentialsTokenRequest
        {
            Address = tokenUrl,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Scope = _options.Scope ?? "fhirUser"
        };

        var response = await _httpClient.RequestClientCredentialsTokenAsync(request, cancellationToken);
        if (response.IsError)
        {
            _logger.LogError("SMART token request failed: {Error}", response.Error);
            return null;
        }

        _cachedToken = response.AccessToken ?? "";
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);
        return _cachedToken;
    }

    private async Task<string?> DiscoverTokenEndpointAsync(CancellationToken cancellationToken)
    {
        var baseUrl = _options.FhirBaseUrl!.TrimEnd('/');
        var url = $"{baseUrl}/.well-known/smart-configuration";
        try
        {
            var resp = await _httpClient.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("token_endpoint", out var tokenEp))
                return tokenEp.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SMART discovery failed for {Url}", url);
        }

        return null;
    }
}
