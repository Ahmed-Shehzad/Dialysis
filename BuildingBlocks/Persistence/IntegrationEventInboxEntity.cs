namespace BuildingBlocks.Persistence;

/// <summary>
/// Inbox row for idempotent integration event consumption. Before processing an inbound message
/// (e.g. from RabbitMQ, Azure Service Bus), check MessageId; if it exists, skip (already processed).
/// </summary>
public sealed class IntegrationEventInboxEntity
{
    public required string MessageId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
    public string? TenantId { get; set; }
    public string? EventType { get; set; }
}
