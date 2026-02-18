using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;

using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Application.Abstractions;

public interface IPatientRepository
{
    Task<DomainPatient?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DomainPatient>> SearchByNameAsync(PersonName name, CancellationToken cancellationToken = default);
    Task<DomainPatient> AddAsync(DomainPatient patient, CancellationToken cancellationToken = default);
}
