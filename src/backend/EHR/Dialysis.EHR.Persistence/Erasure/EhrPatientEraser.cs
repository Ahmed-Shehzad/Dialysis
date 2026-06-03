using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Persistence.Erasure;

/// <summary>
/// EHR contribution to the GDPR Art. 17 erasure pipeline. EHR holds the largest patient surface
/// of any module — Patient root + 16 child aggregates spanning the chart, scheduling, portal,
/// clinical notes, and billing slices. All inherit <see cref="Audit"/>, so erasure is uniform:
/// each set is soft-deleted via <c>ExecuteUpdateAsync</c> on <c>IsDeleted</c>/<c>DeletedAt</c>/
/// <c>DeletedBy</c>, matching the HIE Documents mechanism.
///
/// The <c>Patient</c> root is erased last so that, if an FK constraint between a child and the
/// root rejects the operation (currently none do — children carry PatientId as a column, not a
/// hard FK), the children are already gone and the operation surfaces a clear failure on the
/// root rather than mid-walk.
///
/// Idempotent: re-running on a patient with nothing left returns zero.
/// </summary>
public sealed class EhrPatientEraser : IPatientEraser
{
    private readonly EhrDbContext _ctx;
    private readonly TimeProvider _clock;
    private readonly ILogger<EhrPatientEraser> _logger;

    public EhrPatientEraser(
        EhrDbContext ctx,
        TimeProvider clock,
        ILogger<EhrPatientEraser> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public string ModuleSlug => "ehr";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var now = _clock.GetUtcNow().UtcDateTime;
        var byCategory = new Dictionary<string, int>(StringComparer.Ordinal);

        // PatientChart slice
        await SoftDeleteAsync(_ctx.Allergies.Where(a => a.PatientId == patientId),
            "Allergy", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.ProblemListItems.Where(p => p.PatientId == patientId),
            "ProblemListItem", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.VitalSignReadings.Where(v => v.PatientId == patientId),
            "VitalSignReading", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.Immunizations.Where(i => i.PatientId == patientId),
            "Immunization", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.MedicationStatements.Where(m => m.PatientId == patientId),
            "MedicationStatement", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // Scheduling slice
        await SoftDeleteAsync(_ctx.Appointments.Where(a => a.PatientId == patientId),
            "Appointment", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // PatientPortal slice
        await SoftDeleteAsync(_ctx.PortalAppointmentRequests.Where(p => p.PatientId == patientId),
            "PortalAppointmentRequest", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.SecureMessages.Where(m => m.PatientId == patientId),
            "SecureMessage", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // ClinicalNotes slice
        await SoftDeleteAsync(_ctx.Encounters.Where(e => e.PatientId == patientId),
            "Encounter", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.ClinicalNotes.Where(n => n.PatientId == patientId),
            "ClinicalNote", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.Prescriptions.Where(p => p.PatientId == patientId),
            "Prescription", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.LabOrders.Where(o => o.PatientId == patientId),
            "LabOrder", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.LabResults.Where(r => r.PatientId == patientId),
            "LabResult", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // Billing slice
        await SoftDeleteAsync(_ctx.Charges.Where(c => c.PatientId == patientId),
            "Charge", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.Claims.Where(c => c.PatientId == patientId),
            "Claim", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.Payments.Where(p => p.PatientId == patientId),
            "Payment", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        // Registration root — erased last so child failures don't leave an orphan root.
        await SoftDeleteAsync(_ctx.Patients.Where(p => p.Id == patientId),
            "Patient", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        var total = byCategory.Values.Sum();
        if (total > 0)
        {
            _logger.LogInformation(
                "DSR Art. 17 erasure (EHR) — patient {PatientId}: {Total} row(s) across {Categories} type(s) by {ApprovedBy}.",
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
                    .SetProperty(e => e.DeletedAt, (DateTime?)now)
                    .SetProperty(e => e.DeletedBy, approvedBy),
                cancellationToken)
            .ConfigureAwait(false);
        if (affected > 0)
            byCategory[category] = affected;
    }
}
