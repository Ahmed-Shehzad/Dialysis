using Dialysis.EHR.PatientPortal.Domain;

namespace Dialysis.EHR.PatientPortal.Ports;

public interface IPortalAppointmentRequestRepository
{
    Task<PortalAppointmentRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalAppointmentRequest>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalAppointmentRequest>> ListByStatusAsync(PortalAppointmentRequestStatus status, int take, CancellationToken cancellationToken = default);
    void Add(PortalAppointmentRequest request);
}
