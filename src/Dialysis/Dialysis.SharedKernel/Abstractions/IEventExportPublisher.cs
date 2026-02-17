namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Publishes domain events for external consumers (Kafka, ETL, webhooks). Phase 2.3.2.
/// </summary>
public interface IEventExportPublisher
{
    /// <summary>
    /// Publish event to export pipeline. No-op when not configured.
    /// </summary>
    Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
