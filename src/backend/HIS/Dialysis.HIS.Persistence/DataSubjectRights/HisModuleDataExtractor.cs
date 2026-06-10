using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.DataSubjectRights;

/// <summary>
/// HIS contribution to the GDPR Art. 15 / 20 export pipeline. Structurally mirrors
/// <see cref="Erasure.HisPatientEraser"/>: walks exactly the same patient-linked aggregates the
/// eraser soft- or hard-deletes (Appointment / Admission / MedicationOrder, the RA-capability
/// rows, and raw device readings) but reads them into <see cref="DataSubjectResource"/> entries
/// instead of deleting. Soft-deleted rows are excluded — once the eraser ran they are no longer
/// "personal data undergoing processing" in the Art. 15 sense, only an audit tombstone.
/// Rows serialize via <see cref="DataSubjectExportJson"/> (module-domain JSON, not full FHIR —
/// the aggregator merges them into the operator-facing bundle).
/// </summary>
public sealed class HisModuleDataExtractor : IModuleDataExtractor
{
    private readonly HisDbContext _ctx;

    public HisModuleDataExtractor(HisDbContext ctx) => _ctx = ctx;

    public string ModuleSlug => "his";

    public async Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var resources = new List<DataSubjectResource>();

        // Audit-tracked aggregates — the eraser soft-deletes these, so export only live rows.
        await AddRowsAsync(
            _ctx.Appointments.Where(a => a.PatientId == patientId && !a.IsDeleted),
            "Appointment", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.Admissions.Where(a => a.PatientId == patientId && !a.IsDeleted),
            "Admission", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.MedicationOrders.Where(m => m.PatientId == patientId && !m.IsDeleted),
            "MedicationOrder", m => m.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

        // Non-Audit rows — the eraser hard-deletes these; export whatever is present.
        await AddRowsAsync(
            _ctx.DeviceReadings.Where(d => d.PatientId == patientId),
            "DeviceReadingRecord", d => d.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.RaPatientAlerts.Where(a => a.PatientId == patientId),
            "RaPatientAlert", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.RaWaitlistEntries.Where(w => w.PatientId == patientId),
            "RaWaitlistEntry", w => w.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.RaSpecialistEncounterRecords.Where(r => r.PatientId == patientId),
            "RaSpecialistEncounterRecord", r => r.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.RaEhrDocumentExchangeRecords.Where(r => r.PatientId == patientId),
            "RaEhrDocumentExchangeRecord", r => r.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.RaClinicalDecisionSupportEvaluations.Where(e => e.PatientId == patientId),
            "RaClinicalDecisionSupportEvaluation", e => e.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

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
