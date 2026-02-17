namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

/// <summary>
/// HTTP request DTO for vitals ingestion. Maps from API boundary (primitives) to command (value objects).
/// </summary>
public sealed record IngestVitalsRequest
{
    public required string PatientId { get; init; }
    public int? Systolic { get; init; }
    public int? Diastolic { get; init; }
    public decimal? HeartRate { get; init; }
    public decimal? WeightKg { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}
