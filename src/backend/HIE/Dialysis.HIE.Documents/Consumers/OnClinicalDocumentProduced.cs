using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Documents.Consumers;

/// <summary>
/// Consumes <see cref="ClinicalDocumentProducedIntegrationEvent"/> from PDMS Reporting and
/// indexes the report as a HIE <see cref="DocumentReference"/>. Idempotent on the source
/// <see cref="ClinicalDocumentProducedIntegrationEvent.ReportId"/>: re-delivery of the same
/// event finds the existing row (mapped to <see cref="DocumentReference.Id"/>) and exits
/// without re-indexing.
/// </summary>
public sealed class OnClinicalDocumentProduced : IConsumer<ClinicalDocumentProducedIntegrationEvent>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly ILogger<OnClinicalDocumentProduced> _logger;
    /// <summary>
    /// Consumes <see cref="ClinicalDocumentProducedIntegrationEvent"/> from PDMS Reporting and
    /// indexes the report as a HIE <see cref="DocumentReference"/>. Idempotent on the source
    /// <see cref="ClinicalDocumentProducedIntegrationEvent.ReportId"/>: re-delivery of the same
    /// event finds the existing row (mapped to <see cref="DocumentReference.Id"/>) and exits
    /// without re-indexing.
    /// </summary>
    public OnClinicalDocumentProduced(IDocumentReferenceRepository repository,
        ILogger<OnClinicalDocumentProduced> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<ClinicalDocumentProducedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        var message = context.Message;

        var existing = await _repository.FindAsync(message.ReportId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug("DocumentReference for report {ReportId} already indexed; skipping.", message.ReportId);
            return;
        }

        var doc = new DocumentReference(
            id: message.ReportId,
            patientId: message.PatientId,
            kind: message.Kind,
            title: message.Title,
            mimeType: message.MimeType,
            storageRef: message.StorageRef,
            contentHash: message.ContentHash,
            size: 0,
            source: DocumentReferenceSource.PdmsReporting,
            createdAtUtc: message.OccurredOn,
            createdBy: null,
            languageCode: message.LanguageCode,
            hasAcroForms: false,
            hasJavascript: false);
        var created = await _repository.TryAddIdempotentAsync(doc, ct).ConfigureAwait(false);
        if (!created)
            _logger.LogDebug("DocumentReference for report {ReportId} already indexed (concurrent insert); skipping.", message.ReportId);
    }
}
