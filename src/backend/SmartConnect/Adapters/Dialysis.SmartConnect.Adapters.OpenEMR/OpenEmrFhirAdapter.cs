namespace Dialysis.SmartConnect.Adapters.OpenEMR;

public sealed class OpenEmrAdapterOptions
{
    public required string BaseUrl { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    public string Scope { get; set; } = "openid offline_access patient/*.read";
}

public sealed class OpenEmrAuthProvider(Microsoft.Extensions.Options.IOptions<OpenEmrAdapterOptions> options) : IExternalEhrAuthProvider
{
    private readonly OpenEmrAdapterOptions _options = options.Value;

    public string VendorName => "OpenEMR";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}

public sealed class OpenEmrFhirAdapter(IHttpClientFactory httpClientFactory, OpenEmrAuthProvider authProvider, Microsoft.Extensions.Options.IOptions<OpenEmrAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly OpenEmrAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("OpenEMR", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
