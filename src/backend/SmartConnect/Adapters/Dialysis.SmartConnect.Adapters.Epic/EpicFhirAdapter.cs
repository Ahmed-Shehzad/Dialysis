namespace Dialysis.SmartConnect.Adapters.Epic;

public sealed class EpicAdapterOptions
{
    public required string BaseUrl { get; set; }

    public required string ClientId { get; set; }

    /// <summary>Path to the PEM-encoded private key for Epic OAuth2 JWT-based client auth.</summary>
    public required string PrivateKeyPemPath { get; set; }
}

public sealed class EpicAuthProvider : IExternalEhrAuthProvider
{
    public string VendorName => "Epic";

    // Production wiring: build a signed JWT with the Epic-issued client_id, post to /token, cache the
    // resulting access token by tenant. Skeleton returns an empty token so the adapter can be wired
    // and unit-tested without live Epic credentials.
    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}

public sealed class EpicFhirAdapter(IHttpClientFactory httpClientFactory, EpicAuthProvider authProvider, Microsoft.Extensions.Options.IOptions<EpicAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly EpicAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Epic", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
