using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Dialysis.HIS.PatientAccess.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfPatientPortalReadModel : IPatientPortalReadModel
{
    private readonly HisDbContext _db;
    public EfPatientPortalReadModel(HisDbContext db) => _db = db;
    public async Task<PatientPortalSummaryDto> GetSummaryAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var upcoming = await _db.Appointments.AsNoTracking()
            .Where(a => a.PatientId == patientId && a.StatusCode == "Booked")
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var openOrders = await _db.MedicationOrders.AsNoTracking()
            .Where(o => o.PatientId == patientId && o.StatusCode == "Placed")
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var openAdmissions = await _db.Admissions.AsNoTracking()
            .Where(a => a.PatientId == patientId && a.DischargedAtUtc == null)
            .CountAsync(cancellationToken).ConfigureAwait(false);

        return new PatientPortalSummaryDto(patientId, upcoming, openOrders, openAdmissions);
    }

    public async Task<IReadOnlyList<Guid>> ListPatientIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var cap = Math.Clamp(take, 1, 200);

        // Gather distinct patient ids from each portal-relevant set, then union in memory. Three small
        // capped reads avoid relying on cross-set EF UNION translation, and any of the three is enough
        // to make a patient discoverable.
        var fromAppointments = await _db.Appointments.AsNoTracking()
            .Where(a => a.StatusCode == "Booked")
            .Select(a => a.PatientId).Distinct().Take(cap)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var fromOrders = await _db.MedicationOrders.AsNoTracking()
            .Where(o => o.StatusCode == "Placed")
            .Select(o => o.PatientId).Distinct().Take(cap)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var fromAdmissions = await _db.Admissions.AsNoTracking()
            .Where(a => a.DischargedAtUtc == null)
            .Select(a => a.PatientId).Distinct().Take(cap)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return fromAppointments.Concat(fromOrders).Concat(fromAdmissions)
            .Distinct().Take(cap).ToList();
    }
}
