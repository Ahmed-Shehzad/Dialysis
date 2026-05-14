namespace Dialysis.Hie.Inbound.Domain;

/// <summary>
/// MPI row backing <c>GET /api/v1.0/fhir/Patient/$match</c>. Externally-supplied identifiers + denormalized
/// demographic facts for matching; never the system of record.
/// </summary>
public sealed class PatientIndexEntry
{
    public Guid Id { get; private set; }
    public string PartnerId { get; private set; } = string.Empty;
    public string ExternalLogicalId { get; private set; } = string.Empty;
    public string? MedicalRecordNumber { get; private set; }
    public string? FamilyName { get; private set; }
    public string? GivenName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? SexAtBirthCode { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private PatientIndexEntry() { }

    public PatientIndexEntry(
        string partnerId,
        string externalLogicalId,
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        string? sexAtBirthCode,
        DateTime updatedAtUtc)
    {
        Id = Guid.NewGuid();
        PartnerId = partnerId;
        ExternalLogicalId = externalLogicalId;
        MedicalRecordNumber = medicalRecordNumber;
        FamilyName = familyName;
        GivenName = givenName;
        DateOfBirth = dateOfBirth;
        SexAtBirthCode = sexAtBirthCode;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Refresh(
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        string? sexAtBirthCode,
        DateTime updatedAtUtc)
    {
        MedicalRecordNumber = medicalRecordNumber;
        FamilyName = familyName;
        GivenName = givenName;
        DateOfBirth = dateOfBirth;
        SexAtBirthCode = sexAtBirthCode;
        UpdatedAtUtc = updatedAtUtc;
    }
}
