using Dialysis.HIS.PatientAccess.Ports;
using Dialysis.HIS.Persistence.Stores;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.PatientAccess;

public sealed class EfPatientPortalConsentReadModel(HisDbContext db) : IPatientPortalConsentReadModel
{
    public async Task<PatientPortalConsentState?> GetAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var row = await db.PortalConsentPreferences.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PatientId == patientId, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : new PatientPortalConsentState(row.SummaryVisible, row.AppointmentRequestsAllowed);
    }
}
