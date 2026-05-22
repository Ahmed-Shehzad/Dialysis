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

    public async Task<IReadOnlyList<OutboundBundle>> ListAsync(OutboundBundleStatus? statusFilter, int take, CancellationToken cancellationToken = default)
    {
        var query = db.OutboundBundles.AsNoTracking().AsQueryable();
        if (statusFilter.HasValue)
            query = query.Where(b => b.Status == statusFilter.Value);
        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<OutboundBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.OutboundBundles.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
