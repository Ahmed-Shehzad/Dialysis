namespace Dialysis.FhirStore.Data;

/// <summary>
/// Persisted FHIR Observation (vitals, lab values, etc.).
/// </summary>
public sealed class ObservationEntity
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required string PatientId { get; init; }
    public required string LoincCode { get; init; }
    public string? Display { get; init; }
    public string? Unit { get; init; }
    public string? UnitSystem { get; init; }
    public decimal? NumericValue { get; init; }
    public required DateTimeOffset Effective { get; init; }
    public string? RawJson { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
