using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Persistence.DataSubjectRights;

/// <summary>
/// PDMS contribution to the GDPR Art. 15 / 20 export pipeline. Structurally mirrors
/// <see cref="Erasure.PdmsPatientEraser"/>: projects the patient's session ids first, then reads
/// every per-session child (<c>IntradialyticReading</c>, <c>TreatmentObservation</c>,
/// <c>TreatmentAlarm</c>, <c>IvPumpInfusion</c>, <c>AlarmDispatch</c>), the sessions themselves,
/// and the two direct patient-linked aggregates (<c>MedicationAdministrationRecord</c>,
/// <c>SessionReport</c>). Soft-deleted rows are excluded — after erasure they are tombstones, not
/// personal data undergoing processing. Rows serialize via <see cref="DataSubjectExportJson"/>.
/// Note the readings hypertable can be large for a long-tenured patient — the export is an
/// on-demand operator action, not a hot path.
/// </summary>
public sealed class PdmsModuleDataExtractor : IModuleDataExtractor
{
    private readonly PdmsDbContext _ctx;

    public PdmsModuleDataExtractor(PdmsDbContext ctx) => _ctx = ctx;

    public string ModuleSlug => "pdms";

    public async Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var resources = new List<DataSubjectResource>();

        var sessionIds = await _ctx.Sessions
            .Where(s => s.PatientId == patientId && !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sessionIds.Count > 0)
        {
            await AddRowsAsync(
                _ctx.Sessions.Where(s => sessionIds.Contains(s.Id)),
                "DialysisSession", s => s.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
            await AddRowsAsync(
                _ctx.Readings.Where(r => sessionIds.Contains(r.SessionId) && !r.IsDeleted),
                "IntradialyticReading", r => r.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
            await AddRowsAsync(
                _ctx.TreatmentObservations.Where(o => sessionIds.Contains(o.SessionId) && !o.IsDeleted),
                "TreatmentObservation", o => o.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
            await AddRowsAsync(
                _ctx.TreatmentAlarms.Where(a => a.SessionId.HasValue && sessionIds.Contains(a.SessionId.Value) && !a.IsDeleted),
                "TreatmentAlarm", a => a.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
            await AddRowsAsync(
                _ctx.IvPumpInfusions.Where(i => sessionIds.Contains(i.SessionId) && !i.IsDeleted),
                "IvPumpInfusion", i => i.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
            await AddRowsAsync(
                _ctx.AlarmDispatches.Where(d => sessionIds.Contains(d.SessionId) && !d.IsDeleted),
                "AlarmDispatch", d => d.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        }

        await AddRowsAsync(
            _ctx.MedicationAdministrationRecords.Where(m => m.PatientId == patientId && !m.IsDeleted),
            "MedicationAdministrationRecord", m => m.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);
        await AddRowsAsync(
            _ctx.SessionReports.Where(r => r.PatientId == patientId && !r.IsDeleted),
            "SessionReport", r => r.Id.ToString(), resources, cancellationToken).ConfigureAwait(false);

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
