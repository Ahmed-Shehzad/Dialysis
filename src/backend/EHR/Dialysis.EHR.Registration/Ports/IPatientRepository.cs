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

    /// <summary>
    /// Streams every <see cref="Patient"/> for bulk-export NDJSON output. Ordered by MRN
    /// for stable pagination. The Patient aggregate does not yet carry a last-modified
    /// timestamp, so the FHIR <c>_since</c> filter is best-effort and currently ignored —
    /// the parameter is reserved for the eventual <c>Meta.lastUpdated</c> tracking.
    /// </summary>
    IAsyncEnumerable<Patient> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);

    void Add(Patient patient);
}
