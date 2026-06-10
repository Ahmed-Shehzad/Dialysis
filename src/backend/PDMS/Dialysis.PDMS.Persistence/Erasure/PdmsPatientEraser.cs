using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Persistence.Erasure;

/// <summary>
/// PDMS contribution to the GDPR Art. 17 erasure pipeline. PDMS persists the dialysis-session
/// timeline: the <c>DialysisSession</c> aggregate (PatientId), per-session children that link by
/// <c>SessionId</c> (<c>IntradialyticReading</c>, <c>TreatmentObservation</c>, <c>TreatmentAlarm</c>,
/// <c>IvPumpInfusion</c>, <c>AlarmDispatch</c>), and two direct patient-linked aggregates
/// (<c>MedicationAdministrationRecord</c>, <c>SessionReport</c>).
///
/// Order matters: we look up the patient's session IDs first, soft-delete every per-session
/// child, then the sessions themselves, then the direct rows. Walking parents before children
/// would orphan the children once the SessionId-based filter no longer matches a live row, so we
/// project IDs into memory before the cascade starts.
///
/// Idempotent: re-running on a patient with nothing left returns zero. The session-id projection
/// only loads guids, so the working set stays bounded even for high-frequency telemetry rows.
/// </summary>
public sealed class PdmsPatientEraser : IPatientEraser
{
    private readonly PdmsDbContext _ctx;
    private readonly TimeProvider _clock;
    private readonly ILogger<PdmsPatientEraser> _logger;

    public PdmsPatientEraser(
        PdmsDbContext ctx,
        TimeProvider clock,
        ILogger<PdmsPatientEraser> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public string ModuleSlug => "pdms";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var now = _clock.GetUtcNow().UtcDateTime;
        var byCategory = new Dictionary<string, int>(StringComparer.Ordinal);

        var sessionIds = await _ctx.Sessions
            .Where(s => s.PatientId == patientId && !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sessionIds.Count > 0)
        {
            await SoftDeleteAsync(_ctx.Readings.Where(r => sessionIds.Contains(r.SessionId)),
                "IntradialyticReading", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
            await SoftDeleteAsync(_ctx.TreatmentObservations.Where(o => sessionIds.Contains(o.SessionId)),
                "TreatmentObservation", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
            await SoftDeleteAsync(_ctx.TreatmentAlarms.Where(a => a.SessionId.HasValue && sessionIds.Contains(a.SessionId.Value)),
                "TreatmentAlarm", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
            await SoftDeleteAsync(_ctx.IvPumpInfusions.Where(i => sessionIds.Contains(i.SessionId)),
                "IvPumpInfusion", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
            await SoftDeleteAsync(_ctx.AlarmDispatches.Where(d => sessionIds.Contains(d.SessionId)),
                "AlarmDispatch", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
            await SoftDeleteAsync(_ctx.Sessions.Where(s => sessionIds.Contains(s.Id)),
                "DialysisSession", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        }

        await SoftDeleteAsync(_ctx.MedicationAdministrationRecords.Where(m => m.PatientId == patientId),
            "MedicationAdministrationRecord", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);
        await SoftDeleteAsync(_ctx.SessionReports.Where(r => r.PatientId == patientId),
            "SessionReport", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        var total = byCategory.Values.Sum();
        if (total > 0)
        {
            _logger.LogInformation(
                "DSR Art. 17 erasure (PDMS) — patient {PatientId}: {Total} row(s) across {Categories} type(s) by {ApprovedBy}.",
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
}
