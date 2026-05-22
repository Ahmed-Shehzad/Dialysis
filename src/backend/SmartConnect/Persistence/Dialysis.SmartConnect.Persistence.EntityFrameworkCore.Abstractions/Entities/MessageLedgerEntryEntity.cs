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

    public DateTimeOffset CreatedAtUtc { get; set; }
}
