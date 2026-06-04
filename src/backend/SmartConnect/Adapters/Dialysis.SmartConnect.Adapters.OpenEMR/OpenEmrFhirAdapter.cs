using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Adapters.OpenEMR;

public sealed class OpenEmrAdapterOptions
{
    /// <summary>FHIR R4 base URL, e.g. <c>https://openemr.example.com/apis/default/fhir</c>.</summary>
    public required string BaseUrl { get; set; }

    /// <summary>OAuth2 token endpoint, typically <c>{site}/oauth2/default/token</c>.</summary>
    public required string TokenEndpoint { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    /// <summary>System-level scopes for backend services. Patient-level scopes require a launch context.</summary>
    public string Scope { get; set; } = "system/Patient.read";
}

/// <summary>
/// OpenEMR FHIR R4 backend-services auth via OAuth2 <c>client_credentials</c> grant. OpenEMR accepts
/// the secret as form fields (<c>client_id</c>/<c>client_secret</c>) rather than HTTP Basic — this
/// matches the OpenEMR API gateway documentation.
/// </summary>
public sealed class OpenEmrAuthProvider : IExternalEhrAuthProvider
{
    private readonly OpenEmrAdapterOptions _options;
    private readonly OAuth2TokenAcquirer _tokenAcquirer;
    /// <summary>
    /// OpenEMR FHIR R4 backend-services auth via OAuth2 <c>client_credentials</c> grant. OpenEMR accepts
    /// the secret as form fields (<c>client_id</c>/<c>client_secret</c>) rather than HTTP Basic — this
    /// matches the OpenEMR API gateway documentation.
    /// </summary>
    public OpenEmrAuthProvider(IOptions<OpenEmrAdapterOptions> options, OAuth2TokenAcquirer tokenAcquirer)
    {
        _tokenAcquirer = tokenAcquirer;
        _options = options.Value;
    }

    public string VendorName => "OpenEMR";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var request = new OAuth2TokenRequest(
            VendorName: VendorName,
            TokenEndpoint: _options.TokenEndpoint,
            CacheKey: $"openemr-token::{context.TenantId}",
            FormFields:
            [
                new("grant_type", "client_credentials"),
                new("client_id", _options.ClientId),
                new("client_secret", _options.ClientSecret),
                new("scope", _options.Scope),
            ]);
        return _tokenAcquirer.AcquireAsync(request, cancellationToken);
    }
}

public sealed class OpenEmrFhirAdapter : HttpFhirAdapterBase
{
    private readonly OpenEmrAdapterOptions _options;
    public OpenEmrFhirAdapter(IHttpClientFactory httpClientFactory,
        OpenEmrAuthProvider authProvider,
        IOptions<OpenEmrAdapterOptions> options) : base(httpClientFactory, authProvider) =>
        _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("OpenEMR", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
