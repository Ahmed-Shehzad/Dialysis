using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Adapters.Allscripts;

public sealed class AllscriptsAdapterOptions
{
    /// <summary>FHIR R4 base URL (Veradigm Sunrise / Allscripts FHIR API).</summary>
    public required string BaseUrl { get; set; }

    /// <summary>OAuth2 token endpoint.</summary>
    public required string TokenEndpoint { get; set; }

    /// <summary>Veradigm-issued application name, sent as the <c>AppName</c> request header.</summary>
    public required string AppName { get; set; }

    /// <summary>Veradigm-issued client identifier used in <c>client_id</c> form field.</summary>
    public required string ClientId { get; set; }

    /// <summary>Resource-owner username (Sunrise practitioner / system account).</summary>
    public required string Username { get; set; }

    /// <summary>Resource-owner password for the configured username.</summary>
    public required string Password { get; set; }

    public string Scope { get; set; } = "system/Patient.read";
}

/// <summary>
/// Veradigm / Allscripts uses an OAuth2 <c>password</c> grant (resource-owner) flow with the
/// vendor-issued <c>AppName</c> carried as an HTTP header. Tokens are cached per-tenant + username.
/// </summary>
public sealed class AllscriptsAuthProvider(IOptions<AllscriptsAdapterOptions> options, OAuth2TokenAcquirer tokenAcquirer)
    : IExternalEhrAuthProvider
{
    private readonly AllscriptsAdapterOptions _options = options.Value;

    public string VendorName => "Allscripts";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var request = new OAuth2TokenRequest(
            VendorName: VendorName,
            TokenEndpoint: _options.TokenEndpoint,
            CacheKey: $"allscripts-token::{context.TenantId}::{_options.Username}",
            FormFields:
            [
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("username", _options.Username),
                new KeyValuePair<string, string>("password", _options.Password),
                new KeyValuePair<string, string>("scope", _options.Scope),
            ],
            ExtraHeaders: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["AppName"] = _options.AppName,
            });
        return tokenAcquirer.AcquireAsync(request, cancellationToken);
    }
}

public sealed class AllscriptsFhirAdapter(
    IHttpClientFactory httpClientFactory,
    AllscriptsAuthProvider authProvider,
    IOptions<AllscriptsAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly AllscriptsAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Allscripts", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
