namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Durable inbox for idempotent consumption: acquire before handlers, complete on success, abandon on failure so at-least-once brokers can retry safely when consumers are idempotent.
/// </summary>
public interface ITransponderInboxGate
{
    /// <summary>
    /// Registers this delivery or returns whether handlers should run. Returns <c>false</c> when the key was already completed. Returns <c>true</c> for a new key or an incomplete prior attempt (crash before <see cref="CompleteAsync"/>).
    /// </summary>
    Task<bool> TryAcquireAsync(string deduplicationKey, string routingKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the key as successfully processed.
    /// </summary>
    Task CompleteAsync(string deduplicationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the pending inbox row so a failed handler can be retried by the broker.
    /// </summary>
    Task AbandonAsync(string deduplicationKey, CancellationToken cancellationToken = default);
}
