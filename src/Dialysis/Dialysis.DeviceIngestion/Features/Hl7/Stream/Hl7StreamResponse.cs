namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// Response for HL7 stream ingestion. 202 Accepted.
/// When processing fails, message is stored in DLQ and FailedMessageId is set.
/// </summary>
public sealed record Hl7StreamResponse
{
    public required string MessageId { get; init; }
    public string Status { get; init; } = "Accepted";
    public bool Failed { get; init; }
    public string? FailedMessageId { get; init; }
    public string? Error { get; init; }
}
