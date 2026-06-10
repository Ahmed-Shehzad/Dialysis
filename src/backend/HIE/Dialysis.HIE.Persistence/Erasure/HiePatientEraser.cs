using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.HIE.Documents.Erasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Persistence.Erasure;

/// <summary>
/// Module-wide HIE contribution to the GDPR Art. 17 erasure pipeline. Composes the existing
/// <see cref="HieDocumentsPatientEraser"/> (blob purge + <c>DocumentReference</c> tombstone) and
/// extends coverage to the other patient-keyed HIE tables:
/// <list type="bullet">
///   <item><c>hie_consent.Consents</c> — the patient's cross-organization disclosure consents;</item>
///   <item><c>hie_outbound.OutboundBundles</c> — queued/delivered FHIR disclosures (the
///     <c>FhirJson</c> column carries full PHI);</item>
///   <item><c>hie_openehr.Compositions</c> — openEHR projections of the patient's clinical data.</item>
/// </list>
/// None of these carry the <c>Audit</c> soft-delete columns, so they hard-delete via
/// <c>ExecuteDeleteAsync</c> (one round-trip per table), mirroring how the HIS eraser treats its
/// non-Audit rows. The DSR audit rows themselves (<c>ErasureRequests</c> / <c>RestrictionRequests</c>)
/// are deliberately retained — they are the Art. 17 accountability trail. Inbound MPI rows
/// (<c>PatientIndex</c>, <c>ReceivedResources</c>) are keyed by partner-assigned external ids, not
/// the platform patient id, so they cannot be matched here.
///
/// Registered as the single <see cref="IPatientEraser"/> for the module so the per-module
/// breakdown on <c>ErasureRequest.ExecutionLog</c> stays one coherent <c>"hie"</c> entry.
/// Idempotent: re-running on a patient with nothing left returns zero.
/// </summary>
public sealed class HiePatientEraser : IPatientEraser
{
    private readonly HieDocumentsPatientEraser _documents;
    private readonly HieDbContext _ctx;
    private readonly ILogger<HiePatientEraser> _logger;

    public HiePatientEraser(
        HieDocumentsPatientEraser documents,
        HieDbContext ctx,
        ILogger<HiePatientEraser> logger)
    {
        _documents = documents;
        _ctx = ctx;
        _logger = logger;
    }

    public string ModuleSlug => "hie";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);

        // Documents slice first — it tombstones rather than deletes, so a failure further down
        // never leaves purged blobs unaccounted for.
        var documents = await _documents.EraseAsync(patientId, approvedBy, cancellationToken)
            .ConfigureAwait(false);
        var byCategory = new Dictionary<string, int>(documents.ByCategory, StringComparer.Ordinal);

        await HardDeleteAsync(
            _ctx.Consents.Where(c => c.PatientId == patientId),
            "ConsentRecord", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.OutboundBundles.Where(b => b.PatientId == patientId),
            "OutboundBundle", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.Compositions.Where(c => c.PatientId == patientId),
            "OpenEhrComposition", byCategory, cancellationToken).ConfigureAwait(false);

        var total = byCategory.Values.Sum();
        if (total > 0)
        {
            _logger.LogInformation(
                "DSR Art. 17 erasure (HIE) — patient {PatientId}: {Total} row(s) across {Categories} type(s) by {ApprovedBy}.",
                patientId, total, byCategory.Count, approvedBy);
        }
        return new PatientErasureResult(total, byCategory);
    }

    private static async Task HardDeleteAsync<T>(
        IQueryable<T> query,
        string category,
        Dictionary<string, int> byCategory,
        CancellationToken cancellationToken)
        where T : class
    {
        var affected = await query.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        if (affected > 0)
            byCategory[category] = affected;
    }
}
