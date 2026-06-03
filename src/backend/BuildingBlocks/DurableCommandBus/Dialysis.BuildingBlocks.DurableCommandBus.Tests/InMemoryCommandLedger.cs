using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.DurableCommandBus.Tests;

/// <summary>
/// In-memory <see cref="ICommandLedger"/> for the BuildingBlock tests. Mirrors the EF impl's
/// semantics: <c>TryClaimAsync</c> only adds a row when none exists; mark methods modify the
/// existing row. Tests don't need a DbContext, so this avoids the EF in-memory provider's
/// transaction limitations.
/// </summary>
public sealed class InMemoryCommandLedger : ICommandLedger
{
    private readonly ConcurrentDictionary<Guid, CommandLedgerEntry> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _byCorrelation = new(StringComparer.Ordinal);

    public Task<LedgerClaimResult> TryClaimAsync(DurableCommandEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (_byId.TryGetValue(envelope.CommandId, out var existing))
        {
            var outcome = existing.Status switch
            {
                CommandLedgerStatus.Applied => LedgerClaim.AlreadyApplied,
                CommandLedgerStatus.Failed => LedgerClaim.AlreadyFailed,
                _ => LedgerClaim.PendingRetry,
            };
            return Task.FromResult(new LedgerClaimResult(outcome, existing));
        }
        var entry = new CommandLedgerEntry(
            commandId: envelope.CommandId,
            commandTypeKey: envelope.CommandTypeKey,
            correlationId: envelope.CorrelationId,
            enqueuedAtUtc: envelope.EnqueuedAtUtc,
            requestedBySubject: envelope.RequestedBySubject);
        _byId[envelope.CommandId] = entry;
        _byCorrelation[envelope.CorrelationId] = envelope.CommandId;
        return Task.FromResult(new LedgerClaimResult(LedgerClaim.FirstClaim, null));
    }

    public Task MarkAppliedAsync(Guid commandId, string? resultJson, string consumerInstanceId, CancellationToken cancellationToken)
    {
        if (!_byId.TryGetValue(commandId, out var entry))
            throw new InvalidOperationException($"No ledger entry for {commandId}.");
        entry.MarkApplied(DateTime.UtcNow, resultJson, consumerInstanceId);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid commandId, string failureJson, string consumerInstanceId, CancellationToken cancellationToken)
    {
        if (!_byId.TryGetValue(commandId, out var entry))
            throw new InvalidOperationException($"No ledger entry for {commandId}.");
        entry.MarkFailed(DateTime.UtcNow, failureJson, consumerInstanceId);
        return Task.CompletedTask;
    }

    public Task<CommandLedgerEntry?> FindByCorrelationAsync(string correlationId, CancellationToken cancellationToken)
    {
        if (_byCorrelation.TryGetValue(correlationId, out var id) && _byId.TryGetValue(id, out var entry))
            return Task.FromResult<CommandLedgerEntry?>(entry);
        return Task.FromResult<CommandLedgerEntry?>(null);
    }
}
