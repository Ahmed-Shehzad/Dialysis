using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfPatientAppointmentRequestRepository(HisDbContext db) : IPatientAppointmentRequestRepository
{
    public void Add(PatientAppointmentRequest request) => db.PatientAppointmentRequests.Add(request);
}
