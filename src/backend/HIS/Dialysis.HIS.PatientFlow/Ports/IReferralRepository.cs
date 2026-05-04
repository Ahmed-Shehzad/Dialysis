using Dialysis.HIS.PatientFlow.Domain;

namespace Dialysis.HIS.PatientFlow.Ports;

public interface IReferralRepository
{
    void Add(Referral referral);
}
