using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Billing;

/// <summary>
/// EF-backed <see cref="IChargeIdempotencyStore"/>. Persists one marker per
/// <c>(SessionId, CptCode)</c> so re-delivery of the same
/// <c>DialysisSessionChargeReadyIntegrationEvent</c> short-circuits cleanly across
/// pod restarts and across replicas (unlike the in-memory variant).
///
/// The unique index on <c>(SessionId, CptCode)</c> in the EF configuration is the
/// hard guarantee — a concurrent re-delivery on two replicas at the same instant
/// produces an EF <c>DbUpdateException</c> rather than a duplicate charge.
/// </summary>
public sealed class EfChargeIdempotencyStore : IChargeIdempotencyStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _clock;

    public EfChargeIdempotencyStore(DbContext dbContext, TimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<Guid?> FindChargeIdAsync(Guid sessionId, string cptCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        var marker = await _dbContext.Set<ChargeIdempotencyMarker>()
            .Where(m => m.SessionId == sessionId && m.CptCode == cptCode)
            .Select(m => (Guid?)m.ChargeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return marker;
    }

    public async Task RegisterAsync(Guid sessionId, string cptCode, Guid chargeId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        var marker = new ChargeIdempotencyMarker(sessionId, cptCode, chargeId, _clock.GetUtcNow().UtcDateTime);
        await _dbContext.Set<ChargeIdempotencyMarker>()
            .AddAsync(marker, cancellationToken)
            .ConfigureAwait(false);
        // No SaveChanges here — the consumer's unit-of-work owns the transaction so the
        // charge row + the idempotency marker land in the same commit.
    }
}
