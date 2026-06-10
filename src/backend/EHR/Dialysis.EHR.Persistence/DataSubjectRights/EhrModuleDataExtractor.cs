using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.DataSubjectRights;

/// <summary>
/// EHR contribution to the GDPR Art. 15 / 20 export pipeline. Structurally mirrors
/// <see cref="Erasure.EhrPatientEraser"/>: walks the same Patient root + chart / scheduling /
/// portal / clinical-notes / billing aggregates the eraser soft-deletes, but reads them into
/// <see cref="DataSubjectResource"/> entries instead of deleting. Soft-deleted rows are excluded —
/// after erasure they are tombstones, not personal data undergoing processing. Rows serialize via
/// <see cref="DataSubjectExportJson"/> (module-domain JSON; the aggregator merges per-module
/// slices into the operator-facing bundle).
/// </summary>
public sealed class EhrModuleDataExtractor : IModuleDataExtractor
{
    private readonly EhrDbContext _ctx;

    public EhrModuleDataExtractor(EhrDbContext ctx) => _ctx = ctx;

    public string ModuleSlug => "ehr";

    public async Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var resources = new List<DataSubjectResource>();

        // Registration root first — the export reads top-down even though the eraser deletes
        // bottom-up; ordering here is presentational only.
        await AddRowsAsync(_ctx.Patients.Where(p => p.Id == patientId && !p.IsDeleted),
            "Patient", p => p.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // PatientChart slice
        await AddRowsAsync(_ctx.Allergies.Where(a => a.PatientId == patientId && !a.IsDeleted),
            "Allergy", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.ProblemListItems.Where(p => p.PatientId == patientId && !p.IsDeleted),
            "ProblemListItem", p => p.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.VitalSignReadings.Where(v => v.PatientId == patientId && !v.IsDeleted),
            "VitalSignReading", v => v.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.Immunizations.Where(i => i.PatientId == patientId && !i.IsDeleted),
            "Immunization", i => i.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.MedicationStatements.Where(m => m.PatientId == patientId && !m.IsDeleted),
            "MedicationStatement", m => m.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // Scheduling slice
        await AddRowsAsync(_ctx.Appointments.Where(a => a.PatientId == patientId && !a.IsDeleted),
            "Appointment", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // PatientPortal slice
        await AddRowsAsync(_ctx.PortalAppointmentRequests.Where(p => p.PatientId == patientId && !p.IsDeleted),
            "PortalAppointmentRequest", p => p.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.SecureMessages.Where(m => m.PatientId == patientId && !m.IsDeleted),
            "SecureMessage", m => m.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // ClinicalNotes slice
        await AddRowsAsync(_ctx.Encounters.Where(e => e.PatientId == patientId && !e.IsDeleted),
            "Encounter", e => e.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.ClinicalNotes.Where(n => n.PatientId == patientId && !n.IsDeleted),
            "ClinicalNote", n => n.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.Prescriptions.Where(p => p.PatientId == patientId && !p.IsDeleted),
            "Prescription", p => p.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.LabOrders.Where(o => o.PatientId == patientId && !o.IsDeleted),
            "LabOrder", o => o.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.LabResults.Where(r => r.PatientId == patientId && !r.IsDeleted),
            "LabResult", r => r.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // Billing slice
        await AddRowsAsync(_ctx.Charges.Where(c => c.PatientId == patientId && !c.IsDeleted),
            "Charge", c => c.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.Claims.Where(c => c.PatientId == patientId && !c.IsDeleted),
            "Claim", c => c.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(_ctx.Payments.Where(p => p.PatientId == patientId && !p.IsDeleted),
            "Payment", p => p.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        return resources;
    }

    private static async Task AddRowsAsync<T>(
        IQueryable<T> query,
        string resourceType,
        Func<T, string> identifier,
        List<DataSubjectResource> resources,
        CancellationToken cancellationToken)
        where T : class
    {
        var rows = await query.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var row in rows)
            resources.Add(new DataSubjectResource(resourceType, identifier(row), DataSubjectExportJson.Serialize(row)));
    }
}
