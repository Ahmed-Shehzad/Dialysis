using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Adapters.Cerner;

public sealed class CernerAdapterOptions
{
    /// <summary>FHIR R4 base URL, e.g. <c>https://fhir-ehr-code.cerner.com/r4/{tenant}</c>.</summary>
    public required string BaseUrl { get; set; }

    /// <summary>OAuth2 token endpoint, e.g. <c>https://authorization.cerner.com/tenants/{tenant}/protocols/oauth2/profiles/smart-v1/token</c>.</summary>
    public required string TokenEndpoint { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    /// <summary>Space-delimited scopes. Cerner Backend Services defaults to <c>system/Patient.read system/Observation.read</c>.</summary>
    public string Scope { get; set; } = "system/Patient.read";
}

/// <summary>
/// Cerner FHIR R4 backend-services auth via OAuth2 <c>client_credentials</c> grant with HTTP Basic
/// authentication carrying the client id + secret. Bearer tokens are cached per-tenant.
/// </summary>
public sealed class CernerAuthProvider : IExternalEhrAuthProvider
{
    private readonly CernerAdapterOptions _options;
    private readonly OAuth2TokenAcquirer _tokenAcquirer;
    /// <summary>
    /// Cerner FHIR R4 backend-services auth via OAuth2 <c>client_credentials</c> grant with HTTP Basic
    /// authentication carrying the client id + secret. Bearer tokens are cached per-tenant.
    /// </summary>
    public CernerAuthProvider(IOptions<CernerAdapterOptions> options, OAuth2TokenAcquirer tokenAcquirer)
    {
        _tokenAcquirer = tokenAcquirer;
        _options = options.Value;
    }

    public string VendorName => "Cerner";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var request = new OAuth2TokenRequest(
            VendorName: VendorName,
            TokenEndpoint: _options.TokenEndpoint,
            CacheKey: $"cerner-token::{context.TenantId}",
            FormFields:
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", _options.Scope),
            ],
            BasicAuth: new BasicAuthCredential(_options.ClientId, _options.ClientSecret));
        return _tokenAcquirer.AcquireAsync(request, cancellationToken);
    }
}

public sealed class CernerFhirAdapter : HttpFhirAdapterBase
{
    private readonly CernerAdapterOptions _options;
    public CernerFhirAdapter(IHttpClientFactory httpClientFactory,
        CernerAuthProvider authProvider,
        IOptions<CernerAdapterOptions> options) : base(httpClientFactory, authProvider) =>
        _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Cerner", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
