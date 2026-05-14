namespace Dialysis.SmartConnect.Adapters.Allscripts;

public sealed class AllscriptsAdapterOptions
{
    public required string BaseUrl { get; set; }

    public required string AppName { get; set; }

    public required string Username { get; set; }

    public required string Password { get; set; }
}

public sealed class AllscriptsAuthProvider(Microsoft.Extensions.Options.IOptions<AllscriptsAdapterOptions> options) : IExternalEhrAuthProvider
{
    private readonly AllscriptsAdapterOptions _options = options.Value;

    public string VendorName => "Allscripts";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}

public sealed class AllscriptsFhirAdapter(IHttpClientFactory httpClientFactory, AllscriptsAuthProvider authProvider, Microsoft.Extensions.Options.IOptions<AllscriptsAdapterOptions> options)
    : HttpFhirAdapterBase(httpClientFactory, authProvider)
{
    private readonly AllscriptsAdapterOptions _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Allscripts", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
