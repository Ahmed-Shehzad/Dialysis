using System.Text;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Dicom.Dimse;

/// <summary>
/// fo-dicom DIMSE provider that handles incoming C-STORE associations. One instance per association;
/// fo-dicom calls <see cref="OnReceiveAssociationRequestAsync"/> to decide whether to accept the peer,
/// then <see cref="OnCStoreRequestAsync"/> per instance.
/// </summary>
public sealed class DicomCStoreProvider : DicomService, IDicomServiceProvider, IDicomCStoreProvider
{
    private static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes =
    [
        DicomTransferSyntax.ExplicitVRLittleEndian,
        DicomTransferSyntax.ImplicitVRLittleEndian,
        DicomTransferSyntax.ExplicitVRBigEndian,
    ];

    private readonly DicomCStoreProviderFactory _factory;

    public DicomCStoreProvider(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger log,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, log, dependencies)
    {
        // userState is set on the DicomServer; fo-dicom propagates it via the service base.
        _factory = (DicomCStoreProviderFactory)UserState
            ?? throw new InvalidOperationException("DimseCStoreHostedService must set userState.");
    }

    public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        ArgumentNullException.ThrowIfNull(association);
        var opts = _factory.Options;
        if (!IsAetAccepted(association.CallingAE, opts.AllowedCallingAet))
        {
            return SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CallingAENotRecognized);
        }
        foreach (var pc in association.PresentationContexts)
        {
            pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
        }
        return SendAssociationAcceptAsync(association);
    }

    public Task OnReceiveAssociationReleaseRequestAsync() => SendAssociationReleaseResponseAsync();

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason) =>
        Logger.LogWarning("DIMSE association aborted: {Source}/{Reason}", source, reason);

    public void OnConnectionClosed(Exception? exception)
    {
        if (exception is not null)
        {
            Logger.LogWarning(exception, "DIMSE connection closed with error");
        }
    }

    public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            await using var scope = _factory.ScopeFactory.CreateAsyncScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<IDicomIngestionService>();
            var file = new DicomFile(request.Dataset);
            await ingestion.IngestAsync(file, CancellationToken.None).ConfigureAwait(false);
            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DIMSE C-STORE ingestion failed");
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    public Task OnCStoreRequestExceptionAsync(string? tempFileName, Exception e)
    {
        Logger.LogError(e, "DIMSE C-STORE exception (temp file: {File})", tempFileName);
        return Task.CompletedTask;
    }

    private static bool IsAetAccepted(string callingAet, string allowList)
    {
        if (string.IsNullOrWhiteSpace(allowList) || allowList == "*")
            return true;
        return Array.Exists(
            allowList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            aet => string.Equals(aet, callingAet, StringComparison.OrdinalIgnoreCase));
    }
}
