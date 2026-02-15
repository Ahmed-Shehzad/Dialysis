using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.IngestVitals;

public sealed record IngestVitalsCommand : ICommand<IngestVitalsResult>
{
    public required string PatientId { get; init; }
    public required string EncounterId { get; init; }
    public required string DeviceId { get; init; }
    public required IReadOnlyList<VitalReading> Readings { get; init; }
}

public sealed record VitalReading
{
    public required string Code { get; init; }
    public required string Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset Effective { get; init; }
}
