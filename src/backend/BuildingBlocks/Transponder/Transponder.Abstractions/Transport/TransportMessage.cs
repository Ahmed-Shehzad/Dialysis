namespace Dialysis.BuildingBlocks.Transponder.Transport;

/// <summary>
/// Wire-level payload for <see cref="ITransponderTransport"/>.
/// </summary>
/// <param name="RoutingKey">Broker routing key (typically the message contract identity).</param>
/// <param name="Payload">Serialized message body.</param>
/// <param name="CorrelationId">Optional correlation for tracing and request chains.</param>
/// <param name="ContentType">Optional MIME type; default JSON for Transponder serializers.</param>
/// <param name="DeduplicationId">Optional stable id per logical message (broker message id, etc.) for <see cref="ITransponderInboxGate"/>.</param>
/// <param name="Headers">Optional transport-specific headers (propagated to broker headers where supported).</param>
public readonly record struct TransportMessage(
    string RoutingKey,
    ReadOnlyMemory<byte> Payload,
    string? CorrelationId = null,
    string? ContentType = "application/json",
    string? DeduplicationId = null,
    IReadOnlyDictionary<string, string>? Headers = null);
