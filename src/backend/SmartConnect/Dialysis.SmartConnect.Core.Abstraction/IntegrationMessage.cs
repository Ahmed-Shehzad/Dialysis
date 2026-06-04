using System.Collections.Immutable;

namespace Dialysis.SmartConnect;

/// <summary>
/// A unit of work flowing through SmartConnect (inbound capture through outbound dispatch).
/// </summary>
public sealed class IntegrationMessage
{
    public required Guid Id { get; init; }

    public required Guid FlowId { get; init; }

    /// <summary>Stable identifier for tracing duplicates and retries across the ledger.</summary>
    public required string CorrelationId { get; init; }

    public required ReadOnlyMemory<byte> Payload { get; init; }

    public required PayloadFormat PayloadFormat { get; init; }

    public ImmutableDictionary<string, string> Metadata { get; init; } =
        [];

    public DateTimeOffset ReceivedAtUtc { get; init; }

    public IntegrationMessage CloneWithPayload(ReadOnlyMemory<byte> newPayload, PayloadFormat? format = null) =>
        new()
        {
            Id = Id,
            FlowId = FlowId,
            CorrelationId = CorrelationId,
            Payload = newPayload,
            PayloadFormat = format ?? PayloadFormat,
            Metadata = Metadata,
            ReceivedAtUtc = ReceivedAtUtc,
        };

    /// <summary>Adds or replaces a metadata entry (immutable copy).</summary>
    public IntegrationMessage WithMetadata(string key, string value) =>
        new()
        {
            Id = Id,
            FlowId = FlowId,
            CorrelationId = CorrelationId,
            Payload = Payload,
            PayloadFormat = PayloadFormat,
            Metadata = Metadata.SetItem(key, value),
            ReceivedAtUtc = ReceivedAtUtc,
        };
}
