using Dialysis.EHR.Registration.Domain;

namespace Dialysis.EHR.Registration.Ports;

public sealed record PatientSearchCriteria(
    string? Query,
    string? FamilyName,
    string? GivenName,
    string? MedicalRecordNumber,
    DateOnly? DateOfBirthFrom,
    DateOnly? DateOfBirthTo,
    string? SexAtBirthCode,
    PatientStatus? Status,
    int Skip,
    int Take);

public sealed record PatientSearchPage(IReadOnlyList<Patient> Items, int TotalCount);

public interface IPatientRepository
{
    Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Patient>> SearchAsync(string? nameFragment, int take, CancellationToken cancellationToken = default);

    Task<PatientSearchPage> SearchAsync(PatientSearchCriteria criteria, CancellationToken cancellationToken = default);

    void Add(Patient patient);
}
