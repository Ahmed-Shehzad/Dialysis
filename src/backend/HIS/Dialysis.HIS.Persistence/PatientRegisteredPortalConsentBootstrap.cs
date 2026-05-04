using Dialysis.HIS.Contracts.PatientLifecycle;
using Dialysis.HIS.Persistence.Stores;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence;

/// <summary>Seeds default portal consent when a patient is first registered.</summary>
public sealed class PatientRegisteredPortalConsentBootstrap(HisDbContext db) : IPatientRegisteredLifecycleHook
{
    public async Task AfterPatientRegisteredAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (await db.PortalConsentPreferences.AnyAsync(c => c.PatientId == patientId, cancellationToken).ConfigureAwait(false))
            return;

        db.PortalConsentPreferences.Add(
            new PortalConsentPreference
            {
                PatientId = patientId,
                SummaryVisible = true,
                AppointmentRequestsAllowed = true,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
