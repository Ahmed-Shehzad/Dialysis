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
public sealed class DimseCStoreHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<DimseCStoreOptions> options,
    ILogger<DimseCStoreHostedService> logger) : IHostedService
{
    private IDicomServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var providerFactory = new DicomCStoreProviderFactory(scopeFactory, opts);
        _server = DicomServerFactory.Create<DicomCStoreProvider>(opts.Port, userState: providerFactory);
        logger.LogInformation("DICOM C-STORE SCP listening on port {Port} as {Aet}", opts.Port, opts.CalledAet);
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
internal sealed record DicomCStoreProviderFactory(IServiceScopeFactory ScopeFactory, DimseCStoreOptions Options);
