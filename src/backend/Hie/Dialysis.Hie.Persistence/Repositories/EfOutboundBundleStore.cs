using Dialysis.Hie.Outbound.Domain;
using Dialysis.Hie.Outbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Hie.Persistence.Repositories;

public sealed class EfOutboundBundleStore(HieDbContext db) : IOutboundBundleStore
{
    public Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default) =>
        db.OutboundBundles.AddAsync(bundle, cancellationToken).AsTask();

    public async Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default) =>
        await db.OutboundBundles
            .Where(b => b.Status == OutboundBundleStatus.Pending && b.NextAttemptAtUtc <= asOfUtc)
            .OrderBy(b => b.NextAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
