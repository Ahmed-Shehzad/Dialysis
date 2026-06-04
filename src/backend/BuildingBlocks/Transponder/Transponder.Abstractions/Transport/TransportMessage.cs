namespace Dialysis.BuildingBlocks.Transponder.Transport;

/// <summary>
/// Wire-level payload for <see cref="ITransponderTransport"/>.
/// </summary>
public readonly record struct TransportMessage
{
    /// <summary>
    /// Wire-level payload for <see cref="ITransponderTransport"/>.
    /// </summary>
    /// <param name="RoutingKey">Broker routing key (typically the message contract identity).</param>
    /// <param name="Payload">Serialized message body.</param>
    /// <param name="CorrelationId">Optional correlation for tracing and request chains.</param>
    /// <param name="ContentType">Optional MIME type; default JSON for Transponder serializers.</param>
    /// <param name="DeduplicationId">Optional stable id per logical message (broker message id, etc.) for <see cref="ITransponderInboxGate"/>.</param>
    /// <param name="Headers">Optional transport-specific headers (propagated to broker headers where supported).</param>
    public TransportMessage(string RoutingKey,
        ReadOnlyMemory<byte> Payload,
        string? CorrelationId = null,
        string? ContentType = "application/json",
        string? DeduplicationId = null,
        IReadOnlyDictionary<string, string>? Headers = null)
    {
        this.RoutingKey = RoutingKey;
        this.Payload = Payload;
        this.CorrelationId = CorrelationId;
        this.ContentType = ContentType;
        this.DeduplicationId = DeduplicationId;
        this.Headers = Headers;
    }

    /// <summary>Broker routing key (typically the message contract identity).</summary>
    public string RoutingKey { get; init; }

    /// <summary>Serialized message body.</summary>
    public ReadOnlyMemory<byte> Payload { get; init; }

    /// <summary>Optional correlation for tracing and request chains.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Optional MIME type; default JSON for Transponder serializers.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional stable id per logical message (broker message id, etc.) for <see cref="ITransponderInboxGate"/>.</summary>
    public string? DeduplicationId { get; init; }

    /// <summary>Optional transport-specific headers (propagated to broker headers where supported).</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    public void Deconstruct(out string RoutingKey, out ReadOnlyMemory<byte> Payload, out string? CorrelationId, out string? ContentType, out string? DeduplicationId, out IReadOnlyDictionary<string, string>? Headers)
    {
        RoutingKey = this.RoutingKey;
        Payload = this.Payload;
        CorrelationId = this.CorrelationId;
        ContentType = this.ContentType;
        DeduplicationId = this.DeduplicationId;
        Headers = this.Headers;
    }
}
