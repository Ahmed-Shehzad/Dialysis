using Dialysis.EHR.Registration.Domain;

namespace Dialysis.EHR.Registration.Ports;

public interface IPatientRepository
{
    Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);

    void Add(Patient patient);
}
