using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Adapters.Meditech;

public sealed class MeditechAdapterOptions
{
    /// <summary>FHIR R4 base URL, e.g. <c>https://fhir.meditech.com/v1/{tenant}/r4</c>.</summary>
    public required string BaseUrl { get; set; }

    /// <summary>OAuth2 token endpoint published by the Meditech tenant.</summary>
    public required string TokenEndpoint { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    public string Scope { get; set; } = "system/Patient.read";
}

/// <summary>
/// Meditech Expanse backend-services auth via OAuth2 <c>client_credentials</c> grant with HTTP Basic
/// authentication. Bearer tokens are cached per-tenant.
/// </summary>
public sealed class MeditechAuthProvider : IExternalEhrAuthProvider
{
    private readonly MeditechAdapterOptions _options;
    private readonly OAuth2TokenAcquirer _tokenAcquirer;
    /// <summary>
    /// Meditech Expanse backend-services auth via OAuth2 <c>client_credentials</c> grant with HTTP Basic
    /// authentication. Bearer tokens are cached per-tenant.
    /// </summary>
    public MeditechAuthProvider(IOptions<MeditechAdapterOptions> options, OAuth2TokenAcquirer tokenAcquirer)
    {
        _tokenAcquirer = tokenAcquirer;
        _options = options.Value;
    }

    public string VendorName => "Meditech";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var request = new OAuth2TokenRequest(
            VendorName: VendorName,
            TokenEndpoint: _options.TokenEndpoint,
            CacheKey: $"meditech-token::{context.TenantId}",
            FormFields:
            [
                new("grant_type", "client_credentials"),
                new("scope", _options.Scope),
            ],
            BasicAuth: new BasicAuthCredential(_options.ClientId, _options.ClientSecret));
        return _tokenAcquirer.AcquireAsync(request, cancellationToken);
    }
}

public sealed class MeditechFhirAdapter : HttpFhirAdapterBase
{
    private readonly MeditechAdapterOptions _options;
    public MeditechFhirAdapter(IHttpClientFactory httpClientFactory,
        MeditechAuthProvider authProvider,
        IOptions<MeditechAdapterOptions> options) : base(httpClientFactory, authProvider) =>
        _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Meditech", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
