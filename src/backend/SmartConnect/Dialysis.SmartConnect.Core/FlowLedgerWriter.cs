using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect;

/// <summary>
/// Writes <see cref="MessageLedgerEntry"/> rows for the flow runtime, optionally through a fresh
/// DI scope so parallel outbound dispatch never shares the engine's scoped DbContext across
/// worker tasks.
/// </summary>
internal sealed class FlowLedgerWriter
{
    private readonly IMessageLedger _ledger;
    private readonly TimeProvider _time;
    private readonly IServiceScopeFactory? _scopeFactory;

    /// <summary>
    /// Writes <see cref="MessageLedgerEntry"/> rows for the flow runtime, optionally through a fresh
    /// DI scope so parallel outbound dispatch never shares the engine's scoped DbContext across
    /// worker tasks.
    /// </summary>
    public FlowLedgerWriter(IMessageLedger ledger, TimeProvider time, IServiceScopeFactory? scopeFactory)
    {
        _ledger = ledger;
        _time = time;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Appends one ledger entry for <paramref name="message"/> using the engine's scoped ledger.</summary>
    public Task AppendAsync(
        IntegrationMessage message,
        MessageLedgerStatus status,
        int? outboundRouteOrdinal,
        string? detail,
        byte[]? snapshot,
        CancellationToken cancellationToken) =>
        _ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = message.FlowId,
                IntegrationMessageId = message.Id,
                CorrelationId = message.CorrelationId,
                Status = status,
                OutboundRouteOrdinal = outboundRouteOrdinal,
                Detail = detail,
                PayloadSnapshot = snapshot,
                Metadata = message.Metadata,
                CreatedAtUtc = _time.GetUtcNow(),
            },
            cancellationToken);

    /// <summary>
    /// Like <see cref="AppendAsync"/> but opens a fresh DI scope and resolves the
    /// ledger from it when <paramref name="useScopedLedger"/> is true and a <c>scopeFactory</c>
    /// is wired. Used by parallel outbound dispatch so the engine's scoped DbContext is never
    /// shared across worker tasks (the ChangeTracker race PR #92 fixed for alerts).
    /// </summary>
    public async Task WriteOutboundLedgerScopedAsync(
        IntegrationMessage message,
        int routeOrdinal,
        MessageLedgerStatus status,
        string? detail,
        byte[]? snapshot,
        bool useScopedLedger,
        CancellationToken cancellationToken)
    {
        if (useScopedLedger && _scopeFactory is not null)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var scopedLedger = scope.ServiceProvider.GetService<IMessageLedger>() ?? _ledger;
            await scopedLedger.AppendAsync(
                new MessageLedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    FlowId = message.FlowId,
                    IntegrationMessageId = message.Id,
                    CorrelationId = message.CorrelationId,
                    Status = status,
                    OutboundRouteOrdinal = routeOrdinal,
                    Detail = detail,
                    PayloadSnapshot = snapshot,
                    Metadata = message.Metadata,
                    CreatedAtUtc = _time.GetUtcNow(),
                },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await AppendAsync(message, status, routeOrdinal, detail, snapshot, cancellationToken).ConfigureAwait(false);
    }
}
