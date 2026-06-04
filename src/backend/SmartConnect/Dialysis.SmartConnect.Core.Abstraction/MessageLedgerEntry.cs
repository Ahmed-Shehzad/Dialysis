using System.Collections.Immutable;

namespace Dialysis.SmartConnect;

/// <summary>
/// One append-only row in the <see cref="MessageLedger"/> for an integration message.
/// </summary>
public sealed class MessageLedgerEntry
{
    public required Guid Id { get; init; }

    public required Guid FlowId { get; init; }

    public required Guid IntegrationMessageId { get; init; }

    public required string CorrelationId { get; init; }

    public MessageLedgerStatus Status { get; init; }

    /// <summary>Zero-based index of the outbound route when status relates to an outbound.</summary>
    public int? OutboundRouteOrdinal { get; init; }

    public string? Detail { get; init; }

    public byte[]? PayloadSnapshot { get; init; }

    /// <summary>
    /// Snapshot of the <see cref="IntegrationMessage.Metadata"/> dictionary as observed at this
    /// ledger stage. Stored as a JSON object in the <c>MetadataJson</c> column so the operator
    /// dashboard can filter on sender id / message type / trigger event without having to
    /// re-parse the payload (Slice C of the SmartConnect ↔ Mirth alignment plan).
    /// </summary>
    public ImmutableDictionary<string, string> Metadata { get; init; } =
        [];

    public DateTimeOffset CreatedAtUtc { get; init; }
}
