using Transponder.Abstractions;

namespace Dialysis.Contracts.Messages;

/// <summary>
/// Message received when an HL7v2 message is ingested from the HIS stream.
/// Consumed via Transponder from the hl7-ingest topic.
/// </summary>
public sealed record Hl7Ingested(
    string RawMessage,
    string? MessageType,
    string? TenantId,
    Ulid CorrelationId = default
) : IMessage, ICorrelatedMessage;
