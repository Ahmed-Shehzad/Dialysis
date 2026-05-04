using Dialysis.HIS.PatientFlow.Domain;

namespace Dialysis.HIS.PatientFlow.Ports;

public interface IPatientRepository
{
    Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Patient?> FindByMedicalRecordNumberAsync(string mrn, CancellationToken cancellationToken = default);

    void Add(Patient patient);
}
