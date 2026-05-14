namespace Dialysis.SmartConnect.Adapters.Meditech;

public sealed class MeditechAdapterOptions
{
    public required string BaseUrl { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}

public sealed class MeditechAuthProvider(Microsoft.Extensions.Options.IOptions<MeditechAdapterOptions> options) : IExternalEhrAuthProvider
{
    private readonly MeditechAdapterOptions _options = options.Value;

    public string VendorName => "Meditech";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}

public sealed class MeditechFhirAdapter(IHttpClientFactory httpClientFactory, MeditechAuthProvider authProvider, Microsoft.Extensions.Options.IOptions<MeditechAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly MeditechAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Meditech", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
