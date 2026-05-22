namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class MessageLedgerEntryEntity
{
    public Guid Id { get; set; }

    public Guid FlowId { get; set; }

    public Guid IntegrationMessageId { get; set; }

    public required string CorrelationId { get; set; }

    public int Status { get; set; }

    public int? OutboundRouteOrdinal { get; set; }

    public string? Detail { get; set; }

    public byte[]? PayloadSnapshot { get; set; }

    /// <summary>Serialised <see cref="IntegrationMessage.Metadata"/> snapshot at the time of this
    /// ledger append. Nullable for backward compatibility with rows written before slice C.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Derived from metadata key <c>smartconnect.message-type</c> on append (slice C2).
    /// Indexed for fast dashboard filtering — populated when the inbound transport sets the
    /// metadata key, otherwise <c>null</c>.</summary>
    public string? MessageType { get; set; }

    /// <summary>Derived from metadata key <c>smartconnect.sender</c> on append (slice C2).
    /// Indexed for fast dashboard filtering — populated when the inbound transport sets the
    /// metadata key, otherwise <c>null</c>.</summary>
    public string? SenderId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
