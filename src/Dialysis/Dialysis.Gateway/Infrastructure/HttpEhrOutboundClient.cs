using System.Text;

using Dialysis.SharedKernel.Abstractions;

using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Infrastructure;

public sealed class HttpEhrOutboundClient : IEhrOutboundClient
{
    private readonly HttpClient _httpClient;
    private readonly EhrOutboundOptions _options;
    private readonly ISmartFhirTokenProvider? _tokenProvider;
    private readonly ILogger<HttpEhrOutboundClient> _logger;

    public HttpEhrOutboundClient(
        HttpClient httpClient,
        IOptions<EhrOutboundOptions> options,
        ILogger<HttpEhrOutboundClient> logger,
        ISmartFhirTokenProvider? tokenProvider = null)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.FhirBaseUrl);

    public async Task<EhrPushResult> PushPatientBundleAsync(string patientId, string bundleJson, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new EhrPushResult(false, null, "EHR outbound not configured. Set Integration:EhrFhirBaseUrl.");

        try
        {
            var url = $"{_options.FhirBaseUrl!.TrimEnd('/')}/";
            var content = new StringContent(bundleJson, Encoding.UTF8, "application/fhir+json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            if (_tokenProvider?.IsConfigured == true)
            {
                var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("EHR push succeeded for patient {PatientId}, status {StatusCode}", patientId, (int)response.StatusCode);
                return new EhrPushResult(true, (int)response.StatusCode, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("EHR push failed for patient {PatientId}, status {StatusCode}, error {Error}", patientId, (int)response.StatusCode, errorBody);
            return new EhrPushResult(false, (int)response.StatusCode, errorBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EHR push failed for patient {PatientId}", patientId);
            return new EhrPushResult(false, null, ex.Message);
        }
    }
}

public sealed class EhrOutboundOptions
{
    public const string Section = "Integration";
    public string? FhirBaseUrl { get; set; }

    /// <summary>PDMS FHIR base URL for building resource references in push bundles (e.g. https://pdms.example.com/fhir/r4). Used by SessionCompletionSaga for EHR push when no HTTP context.</summary>
    public string? PdmsFhirBaseUrl { get; set; }

    /// <summary>SMART on FHIR: OAuth2 token endpoint (or discover from FhirBaseUrl/.well-known/smart-configuration).</summary>
    public string? TokenEndpoint { get; set; }
    /// <summary>SMART on FHIR: OAuth2 client credentials.</summary>
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    /// <summary>Optional scope for FHIR access (e.g. system/Patient.read).</summary>
    public string? Scope { get; set; }
}
