namespace Dialysis.FhirStore.Data;

/// <summary>
/// Persisted FHIR Patient.
/// </summary>
public sealed class PatientEntity
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public string? FamilyName { get; init; }
    public string? GivenNames { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? RawJson { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
