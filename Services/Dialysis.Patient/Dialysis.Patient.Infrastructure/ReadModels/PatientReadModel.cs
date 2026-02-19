namespace Dialysis.Patient.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of Patient for query operations.
/// Maps to the Patients table.
/// </summary>
public sealed class PatientReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string MedicalRecordNumber { get; init; } = string.Empty;
    public string? PersonNumber { get; init; }
    public string? SocialSecurityNumber { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
}
