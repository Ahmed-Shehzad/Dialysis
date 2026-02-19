namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Read-only store for Patient queries. Used by query handlers instead of the write repository.
/// </summary>
public interface IPatientReadStore
{
    Task<PatientReadDto?> GetByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default);
    Task<PatientReadDto?> GetByIdAsync(string tenantId, string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientReadDto>> SearchAsync(string tenantId, string? identifier, string? familyName, string? givenName, DateOnly? birthdate, int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for patient query results.
/// </summary>
public sealed record PatientReadDto(
    string Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Gender);
