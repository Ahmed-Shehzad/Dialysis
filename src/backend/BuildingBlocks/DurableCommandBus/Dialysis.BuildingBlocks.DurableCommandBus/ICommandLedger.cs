namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-module idempotency + status store. Implementations track command lifecycle in the SAME
/// DbContext as the aggregate change, so writing the ledger and applying the handler share one
/// EF transaction. The bus building block ships <c>EfCommandLedger&lt;TContext&gt;</c>; tests use
/// <c>InMemoryCommandLedger</c>.
/// </summary>
public interface ICommandLedger
{
    /// <summary>
    /// Tries to claim a command for processing. Returns <see cref="LedgerClaim.FirstClaim"/>
    /// when this is a fresh enqueue (no prior row), <see cref="LedgerClaim.AlreadyApplied"/>
    /// when a prior delivery has already committed an Applied row (the consumer should
    /// republish that row's result and ack), or <see cref="LedgerClaim.PendingRetry"/> when a
    /// prior delivery left a Pending row (consumer was killed mid-handler; safe to retry the
    /// handler since the prior transaction never committed).
    /// </summary>
    Task<LedgerClaimResult> TryClaimAsync(
        DurableCommandEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the row as Applied. Must be called inside the same EF transaction as the handler's
    /// aggregate change (the bus's pipeline behavior ensures this).
    /// </summary>
    Task MarkAppliedAsync(
        Guid commandId, string? resultJson, string consumerInstanceId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the row as Failed. Called after the handler throws a non-transient error and the
    /// consumer is about to dead-letter the envelope.
    /// </summary>
    Task MarkFailedAsync(
        Guid commandId, string failureJson, string consumerInstanceId, CancellationToken cancellationToken);

    /// <summary>Returns the row by correlation id. Used by the status endpoint.</summary>
    Task<CommandLedgerEntry?> FindByCorrelationAsync(
        string correlationId, CancellationToken cancellationToken);
}

/// <summary>
/// Outcome of <see cref="ICommandLedger.TryClaimAsync"/>.
/// </summary>
public enum LedgerClaim
{
    FirstClaim,
    PendingRetry,
    AlreadyApplied,
    AlreadyFailed,
}

/// <summary>
/// Carries the claim outcome plus the pre-existing entry when it's not a first claim. The
/// consumer needs the previous result to ack a redelivery without re-running the handler.
/// </summary>
public sealed record LedgerClaimResult(LedgerClaim Outcome, CommandLedgerEntry? Existing);
