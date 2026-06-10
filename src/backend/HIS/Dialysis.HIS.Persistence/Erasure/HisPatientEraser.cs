using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Persistence.Erasure;

/// <summary>
/// HIS contribution to the GDPR Art. 17 erasure pipeline. Walks every patient-linked aggregate
/// the module persists and either soft-deletes it (when the aggregate inherits the <see cref="Audit"/>
/// primitive — Appointment, Admission, MedicationOrder) or hard-deletes the row (RA-capabilities
/// entities + device readings, which sit outside the Audit base). Uses <c>ExecuteUpdateAsync</c>
/// / <c>ExecuteDeleteAsync</c> for one round-trip per entity type instead of loading every row —
/// important because a long-tenured patient may have thousands of device readings.
///
/// Idempotent: re-running on a patient with nothing left returns zero. Records erased are
/// counted per CLR type (e.g. <c>{ "Appointment": 12, "DeviceReadingRecord": 4730 }</c>) so the
/// composite audit row on <c>ErasureRequest.ExecutionLog</c> answers "show me what was deleted"
/// without replaying the operation.
/// </summary>
public sealed class HisPatientEraser : IPatientEraser
{
    private readonly HisDbContext _ctx;
    private readonly TimeProvider _clock;
    private readonly ILogger<HisPatientEraser> _logger;

    public HisPatientEraser(
        HisDbContext ctx,
        TimeProvider clock,
        ILogger<HisPatientEraser> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public string ModuleSlug => "his";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var now = _clock.GetUtcNow().UtcDateTime;
        var byCategory = new Dictionary<string, int>(StringComparer.Ordinal);

        // Audit-tracked aggregates → soft-delete (matches HIE Documents pattern).
        await SoftDeleteAsync(
            _ctx.Appointments.Where(a => a.PatientId == patientId),
            "Appointment", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(
            _ctx.Admissions.Where(a => a.PatientId == patientId),
            "Admission", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(
            _ctx.MedicationOrders.Where(m => m.PatientId == patientId),
            "MedicationOrder", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // Non-Audit entities (no IsDeleted column) → hard delete. RA-capability rows are
        // operational projections; device readings are raw telemetry — neither carries clinical
        // weight that would warrant preserving a soft-deleted row.
        await HardDeleteAsync(
            _ctx.DeviceReadings.Where(d => d.PatientId == patientId),
            "DeviceReadingRecord", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.RaPatientAlerts.Where(a => a.PatientId == patientId),
            "RaPatientAlert", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.RaWaitlistEntries.Where(w => w.PatientId == patientId),
            "RaWaitlistEntry", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.RaSpecialistEncounterRecords.Where(r => r.PatientId == patientId),
            "RaSpecialistEncounterRecord", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.RaEhrDocumentExchangeRecords.Where(r => r.PatientId == patientId),
            "RaEhrDocumentExchangeRecord", byCategory, cancellationToken).ConfigureAwait(false);
        await HardDeleteAsync(
            _ctx.RaClinicalDecisionSupportEvaluations.Where(e => e.PatientId == patientId),
            "RaClinicalDecisionSupportEvaluation", byCategory, cancellationToken).ConfigureAwait(false);

        var total = byCategory.Values.Sum();
        if (total > 0)
        {
            _logger.LogInformation(
                "DSR Art. 17 erasure (HIS) — patient {PatientId}: {Total} row(s) across {Categories} type(s) by {ApprovedBy}.",
                patientId, total, byCategory.Count, approvedBy);
        }
        return new PatientErasureResult(total, byCategory);
    }

    private static async Task SoftDeleteAsync<T>(
        IQueryable<T> query,
        string category,
        DateTime now,
        string approvedBy,
        Dictionary<string, int> byCategory,
        CancellationToken cancellationToken)
        where T : Audit
    {
        var affected = await query
            .Where(e => !e.IsDeleted)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(e => e.IsDeleted, true)
                    .SetProperty(e => e.DeletedAt, now)
                    .SetProperty(e => e.DeletedBy, approvedBy),
                cancellationToken)
            .ConfigureAwait(false);
        if (affected > 0)
            byCategory[category] = affected;
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
