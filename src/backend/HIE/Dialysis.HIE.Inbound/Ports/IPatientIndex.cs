using Dialysis.HIE.Inbound.Domain;

namespace Dialysis.HIE.Inbound.Ports;

public interface IPatientIndex
{
    /// <summary>Inserts or refreshes the entry and returns the persisted row (its id is stable for linking).</summary>
    Task<PatientIndexEntry> UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientIndexEntry>> MatchAsync(
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocking pass for probabilistic matching: returns the candidate set worth scoring (entries
    /// sharing an MRN, a date of birth, or a family name with the criteria) rather than every row.
    /// The caller scores + ranks the candidates with <c>PatientMatchScorer</c>.
    /// </summary>
    Task<IReadOnlyList<PatientIndexEntry>> MatchCandidatesAsync(
        string? medicalRecordNumber,
        string? familyName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default);
}
