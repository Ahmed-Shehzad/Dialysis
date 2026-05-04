using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfReferralRepository(HisDbContext db) : IReferralRepository
{
    public void Add(Referral referral) => db.Referrals.Add(referral);
}
