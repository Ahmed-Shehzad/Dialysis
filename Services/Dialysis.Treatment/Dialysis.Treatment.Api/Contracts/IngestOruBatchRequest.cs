namespace Dialysis.Treatment.Api.Contracts;

/// <summary>
/// Request to ingest an HL7 batch containing ORU^R01 messages (run sheet capture).
/// </summary>
public sealed record IngestOruBatchRequest(string RawHl7Batch);
