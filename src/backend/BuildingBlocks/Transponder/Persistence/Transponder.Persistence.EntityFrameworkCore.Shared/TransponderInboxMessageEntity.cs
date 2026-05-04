namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Durable inbox row for consumer-side deduplication (same logical message id as produced by publishers or brokers).
/// </summary>
public sealed class TransponderInboxMessageEntity
{
    public Guid Id { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;

    public string RoutingKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
