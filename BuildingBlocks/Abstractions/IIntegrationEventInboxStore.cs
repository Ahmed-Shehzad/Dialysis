namespace BuildingBlocks.Abstractions;

/// <summary>
/// Store for idempotent integration event consumption. Before processing an inbound message
/// (e.g. from RabbitMQ, Azure Service Bus), call <see cref="ExistsAsync"/>; if true, skip.
/// After processing (in the same transaction as business data), call <see cref="AddAsync"/>.
/// </summary>
public interface IIntegrationEventInboxStore
{
    /// <summary>
    /// Returns true if the message has already been processed (idempotent skip).
    /// </summary>
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the message was processed. Call in the same transaction as business logic.
    /// </summary>
    Task AddAsync(string messageId, string? eventType, string? tenantId, CancellationToken cancellationToken = default);
}
