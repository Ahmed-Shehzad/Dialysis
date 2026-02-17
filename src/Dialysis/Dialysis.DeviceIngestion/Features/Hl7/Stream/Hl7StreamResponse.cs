namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// Response for HL7 stream ingestion. 202 Accepted.
/// </summary>
public sealed record Hl7StreamResponse
{
    public required string MessageId { get; init; }
    public string Status { get; init; } = "Accepted";
}
