using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// EF-backed <see cref="ICommandLedger"/> writing to the module's own <c>DbContext</c>. Same DI
/// scope as the handler, so the row insert and the aggregate update share an EF transaction
/// when wrapped by <see cref="DurableCommandLedgerBehavior{TCommand,TResult}"/>.
/// </summary>
public sealed class EfCommandLedger<TContext> : ICommandLedger
    where TContext : DbContext
{
    private readonly TContext _db;

    public EfCommandLedger(TContext db) => _db = db;

    public async Task<LedgerClaimResult> TryClaimAsync(
        DurableCommandEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var existing = await _db.Set<CommandLedgerEntry>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CommandId == envelope.CommandId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            var outcome = existing.Status switch
            {
                CommandLedgerStatus.Applied => LedgerClaim.AlreadyApplied,
                CommandLedgerStatus.Failed => LedgerClaim.AlreadyFailed,
                _ => LedgerClaim.PendingRetry,
            };
            return new LedgerClaimResult(outcome, existing);
        }

        var entry = new CommandLedgerEntry(
            commandId: envelope.CommandId,
            commandTypeKey: envelope.CommandTypeKey,
            correlationId: envelope.CorrelationId,
            enqueuedAtUtc: envelope.EnqueuedAtUtc,
            requestedBySubject: envelope.RequestedBySubject);
        await _db.Set<CommandLedgerEntry>().AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return new LedgerClaimResult(LedgerClaim.FirstClaim, null);
    }

    public async Task MarkAppliedAsync(
        Guid commandId, string? resultJson, string consumerInstanceId, CancellationToken cancellationToken)
    {
        var entry = await _db.Set<CommandLedgerEntry>()
            .FirstAsync(e => e.CommandId == commandId, cancellationToken)
            .ConfigureAwait(false);
        entry.MarkApplied(DateTime.UtcNow, resultJson, consumerInstanceId);
    }

    public async Task MarkFailedAsync(
        Guid commandId, string failureJson, string consumerInstanceId, CancellationToken cancellationToken)
    {
        var entry = await _db.Set<CommandLedgerEntry>()
            .FirstAsync(e => e.CommandId == commandId, cancellationToken)
            .ConfigureAwait(false);
        entry.MarkFailed(DateTime.UtcNow, failureJson, consumerInstanceId);
    }

    public Task<CommandLedgerEntry?> FindByCorrelationAsync(
        string correlationId, CancellationToken cancellationToken) =>
            _db.Set<CommandLedgerEntry>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CorrelationId == correlationId, cancellationToken);
}
