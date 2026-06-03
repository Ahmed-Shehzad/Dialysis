using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Documents.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Documents.Erasure;

/// <summary>
/// HIE's contribution to the GDPR Art. 17 erasure pipeline. When an operator approves an
/// erasure request, the orchestrator (<c>DefaultDataSubjectRightsService</c>) runs every
/// registered <see cref="IPatientEraser"/> in sequence; this implementation:
/// <list type="bullet">
///   <item>lists every <c>Current</c> document the patient owns across all sources;</item>
///   <item>deletes the underlying blob bytes via <see cref="IDocumentBlobStore.DeleteAsync"/>;</item>
///   <item>tombstones the storage ref + soft-deletes the row via
///         <c>DocumentReference.MarkBlobPurged("erasure")</c>.</item>
/// </list>
/// The returned <see cref="PatientErasureResult"/> reports the count + per-source breakdown
/// so the audit row recorded on <c>ErasureRequest.ExecutionLog</c> answers the regulator's
/// "show me what was deleted" question without replaying the operation.
/// </summary>
public sealed class HieDocumentsPatientEraser : IPatientEraser
{
    private readonly IDocumentReferenceRepository _documents;
    private readonly IDocumentBlobStore _blobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HieDocumentsPatientEraser> _logger;

    public HieDocumentsPatientEraser(
        IDocumentReferenceRepository documents,
        IDocumentBlobStore blobs,
        IUnitOfWork unitOfWork,
        ILogger<HieDocumentsPatientEraser> logger)
    {
        _documents = documents;
        _blobs = blobs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public string ModuleSlug => "hie";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId, string approvedBy, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var docs = await _documents.ListForPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        if (docs.Count == 0)
        {
            return new PatientErasureResult(0, new Dictionary<string, int>());
        }

        var byCategory = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var doc in docs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _blobs.DeleteAsync(doc.StorageRef, cancellationToken).ConfigureAwait(false);
            doc.MarkBlobPurged("erasure");
            var key = doc.Source.ToString();
            byCategory[key] = byCategory.GetValueOrDefault(key) + 1;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "DSR Art. 17 erasure — patient {PatientId}: {Count} document(s) purged by {ApprovedBy}.",
            patientId, docs.Count, approvedBy);
        return new PatientErasureResult(docs.Count, byCategory);
    }
}
