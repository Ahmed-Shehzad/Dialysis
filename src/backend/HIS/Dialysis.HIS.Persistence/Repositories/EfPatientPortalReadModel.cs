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
        var nowUtc = DateTime.UtcNow;
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
}
