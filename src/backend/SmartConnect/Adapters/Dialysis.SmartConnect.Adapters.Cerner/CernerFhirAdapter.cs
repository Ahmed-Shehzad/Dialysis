namespace Dialysis.SmartConnect.Adapters.Cerner;

public sealed class CernerAdapterOptions
{
    public required string BaseUrl { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}

public sealed class CernerAuthProvider(Microsoft.Extensions.Options.IOptions<CernerAdapterOptions> options) : IExternalEhrAuthProvider
{
    private readonly CernerAdapterOptions _options = options.Value;

    public string VendorName => "Cerner";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}

public sealed class CernerFhirAdapter(IHttpClientFactory httpClientFactory, CernerAuthProvider authProvider, Microsoft.Extensions.Options.IOptions<CernerAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly CernerAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Cerner", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
