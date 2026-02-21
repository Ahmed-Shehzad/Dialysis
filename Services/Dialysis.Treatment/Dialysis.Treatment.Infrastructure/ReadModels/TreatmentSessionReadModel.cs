namespace Dialysis.Treatment.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of TreatmentSession for query operations.
/// </summary>
public sealed class TreatmentSessionReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string? PatientMrn { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceEui64 { get; init; }
    public string? TherapyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public string? SignedBy { get; init; }
}
