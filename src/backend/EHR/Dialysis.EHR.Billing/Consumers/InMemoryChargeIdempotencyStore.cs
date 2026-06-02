using System.Collections.Concurrent;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Process-scoped in-memory idempotency tracker. Production deployments register an
/// EF-backed implementation (`EfChargeIdempotencyStore`, lands in PR 7) so the map
/// survives restarts and is shared across replicas; the in-memory variant exists so a
/// dev host can run the charge bridge without an EHR Postgres handy.
///
/// Single-process correctness only — a multi-replica deployment running the in-memory
/// store would dedupe per replica, not globally. The composition root surfaces a
/// startup warning when the in-memory variant is registered in production.
/// </summary>
public sealed class InMemoryChargeIdempotencyStore : IChargeIdempotencyStore
{
    private readonly ConcurrentDictionary<(Guid SessionId, string CptCode), Guid> _seen = new();

    public Task<Guid?> FindChargeIdAsync(Guid sessionId, string cptCode, CancellationToken cancellationToken)
        => Task.FromResult(_seen.TryGetValue((sessionId, cptCode), out var id) ? (Guid?)id : null);

    public Task RegisterAsync(Guid sessionId, string cptCode, Guid chargeId, CancellationToken cancellationToken)
    {
        _seen[(sessionId, cptCode)] = chargeId;
        return Task.CompletedTask;
    }
}
