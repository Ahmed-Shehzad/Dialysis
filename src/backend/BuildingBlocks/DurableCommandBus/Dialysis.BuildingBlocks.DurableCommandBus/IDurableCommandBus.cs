using Dialysis.CQRS.Commands;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Application-facing entry point for opt-in durable command dispatch. Endpoint handlers call
/// <see cref="EnqueueAsync{TCommand,TResult}"/> instead of <c>ICqrsGateway.SendCommandAsync</c>;
/// the bus publishes the envelope through the durable transport (publisher confirms,
/// persistent messages) and returns the acceptance token the client uses to poll the status
/// endpoint.
/// </summary>
public interface IDurableCommandBus
{
    /// <summary>
    /// Wraps <paramref name="command"/> in a <see cref="DurableCommandEnvelope"/> and publishes
    /// it via the durable transport. Throws <see cref="DurableCommandException"/> when the
    /// command type isn't registered in the catalog or when the publisher-confirm leg fails;
    /// callers should map the latter to HTTP 503 + Retry-After.
    /// </summary>
    /// <param name="commandId">
    /// Optional client-supplied id. When omitted, a server-side <c>Guid.CreateVersion7()</c>
    /// is used. Same id across retries → at most one applied effect (ledger idempotency).
    /// </param>
    Task<DurableCommandAcceptance> EnqueueAsync<TCommand, TResult>(
        TCommand command,
        Guid? commandId = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
