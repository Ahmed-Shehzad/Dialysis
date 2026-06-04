using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Dicom.Dimse;

/// <summary>
/// Hosts a DIMSE C-STORE SCP (PS3.7 §9.1.2.2) using fo-dicom's <see cref="DicomServer"/>. Legacy
/// dialysis-machine modalities and PACS that don't speak DICOMweb push studies here over TCP;
/// each accepted association forwards instances to <see cref="IDicomIngestionService"/>, which
/// shares the same blob + metadata pipeline as STOW-RS.
/// </summary>
/// <remarks>
/// The Provider class is a per-association DIMSE handler that fo-dicom instantiates. We inject the
/// service-scope factory rather than scoped services directly because the provider's lifetime is
/// tied to the TCP association, not an ASP.NET request scope.
/// </remarks>
public sealed class DimseCStoreHostedService : IHostedService
{
    private IDicomServer? _server;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DimseCStoreOptions> _options;
    private readonly ILogger<DimseCStoreHostedService> _logger;
    /// <summary>
    /// Hosts a DIMSE C-STORE SCP (PS3.7 §9.1.2.2) using fo-dicom's <see cref="DicomServer"/>. Legacy
    /// dialysis-machine modalities and PACS that don't speak DICOMweb push studies here over TCP;
    /// each accepted association forwards instances to <see cref="IDicomIngestionService"/>, which
    /// shares the same blob + metadata pipeline as STOW-RS.
    /// </summary>
    /// <remarks>
    /// The Provider class is a per-association DIMSE handler that fo-dicom instantiates. We inject the
    /// service-scope factory rather than scoped services directly because the provider's lifetime is
    /// tied to the TCP association, not an ASP.NET request scope.
    /// </remarks>
    public DimseCStoreHostedService(IServiceScopeFactory scopeFactory,
        IOptions<DimseCStoreOptions> options,
        ILogger<DimseCStoreHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        var providerFactory = new DicomCStoreProviderFactory(_scopeFactory, opts);
        _server = DicomServerFactory.Create<DicomCStoreProvider>(opts.Port, userState: providerFactory);
        _logger.LogInformation("DICOM C-STORE SCP listening on port {Port} as {Aet}", opts.Port, opts.CalledAet);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Dispose();
        _server = null;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Per-association factory passed via fo-dicom's userState. Wraps the dependencies the provider
/// needs to resolve at construction time — a clean alternative to the static-service-locator
/// pattern fo-dicom's API otherwise nudges callers toward.
/// </summary>
internal sealed record DicomCStoreProviderFactory
{
    /// <summary>
    /// Per-association factory passed via fo-dicom's userState. Wraps the dependencies the provider
    /// needs to resolve at construction time — a clean alternative to the static-service-locator
    /// pattern fo-dicom's API otherwise nudges callers toward.
    /// </summary>
    public DicomCStoreProviderFactory(IServiceScopeFactory ScopeFactory, DimseCStoreOptions Options)
    {
        this.ScopeFactory = ScopeFactory;
        this.Options = Options;
    }
    public IServiceScopeFactory ScopeFactory { get; init; }
    public DimseCStoreOptions Options { get; init; }
    public void Deconstruct(out IServiceScopeFactory ScopeFactory, out DimseCStoreOptions Options)
    {
        ScopeFactory = this.ScopeFactory;
        Options = this.Options;
    }
}
