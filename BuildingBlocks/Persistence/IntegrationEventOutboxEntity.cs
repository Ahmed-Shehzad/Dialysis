namespace BuildingBlocks.Persistence;

/// <summary>
/// Outbox row for integration events. Persisted in the same transaction as business data;
/// background publisher reads and publishes to Transponder, then marks ProcessedAtUtc.
/// </summary>
public sealed class IntegrationEventOutboxEntity
{
    public Ulid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? Error { get; set; }
}
