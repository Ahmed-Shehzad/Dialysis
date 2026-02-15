namespace Dialysis.Contracts.Events;

public sealed record SessionStarted
{
    public required string EncounterId { get; init; }
    public required string PatientId { get; init; }
    public required string DeviceId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
}
