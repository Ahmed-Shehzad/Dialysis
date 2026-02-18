using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;

using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Application.Abstractions;

public interface IPatientRepository : IRepository<DomainPatient>
{
    Task<DomainPatient?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default);
    Task<DomainPatient?> GetByPersonNumberAsync(string personNumber, CancellationToken cancellationToken = default);
    Task<DomainPatient?> GetBySsnAsync(string socialSecurityNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DomainPatient>> SearchByNameAsync(Person name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DomainPatient>> SearchByLastNameAsync(string lastName, CancellationToken cancellationToken = default);
}
