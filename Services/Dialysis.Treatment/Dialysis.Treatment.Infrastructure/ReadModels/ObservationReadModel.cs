namespace Dialysis.Treatment.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of Observation for query operations.
/// </summary>
public sealed class ObservationReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TreatmentSessionId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Value { get; init; }
    public string? Unit { get; init; }
    public string? SubId { get; init; }
    public string? ReferenceRange { get; init; }
    public string? Provenance { get; init; }
    public DateTimeOffset? EffectiveTime { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}
