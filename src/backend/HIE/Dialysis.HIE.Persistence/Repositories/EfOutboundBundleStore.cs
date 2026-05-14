using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

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
