using Dialysis.Hie.Inbound.Domain;

namespace Dialysis.Hie.Inbound.Ports;

public interface IPatientIndex
{
    Task UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientIndexEntry>> MatchAsync(
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default);
}
