namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// HTTP request for raw HL7 v2 message ingestion. Sent from Mirth Connect.
/// </summary>
public sealed record Hl7StreamRequest
{
    public required string RawMessage { get; init; }
}
