using Dialysis.EHR.PatientPortal.Domain;

namespace Dialysis.EHR.PatientPortal.Ports;

public interface ISecureMessageRepository
{
    Task<SecureMessage?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecureMessage>> ListByThreadAsync(Guid threadId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecureMessage>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(SecureMessage message);
}
