using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfReceivedResourceStore : IReceivedResourceStore
{
    private readonly HieDbContext _db;
    public EfReceivedResourceStore(HieDbContext db) => _db = db;
    public async Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // Fast-path dedup: an equal (PartnerId, ResourceType, LogicalId) row already exists.
        var exists = await _db.ReceivedResources
            .AnyAsync(
                r => r.PartnerId == resource.PartnerId
                    && r.ResourceType == resource.ResourceType
                    && r.LogicalId == resource.LogicalId,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return;

        // Own the save (eager, isolated) so a concurrent insert that loses the race against the
        // unique UX_ReceivedResources_PartnerLogicalId index can't poison the batched SaveChanges in
        // InboundIngestionService. The trailing batched flush still persists the rest of the work.
        await _db.ReceivedResources.AddAsync(resource, cancellationToken).ConfigureAwait(false);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // A concurrent ingest committed the same key between our read and this save. Detach the
            // doomed insert and treat the conflict as idempotent — unless the re-read can't find a
            // winner, in which case the failure was something else and must surface.
            _db.Entry(resource).State = EntityState.Detached;
            var winner = await _db.ReceivedResources
                .AsNoTracking()
                .AnyAsync(
                    r => r.PartnerId == resource.PartnerId
                        && r.ResourceType == resource.ResourceType
                        && r.LogicalId == resource.LogicalId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!winner)
                throw;
        }
    }

    public async Task<IReadOnlyList<ReceivedResource>> ListRecentAsync(string? partnerId, int take, CancellationToken cancellationToken = default)
    {
        var query = _db.ReceivedResources.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partnerId))
            query = query.Where(r => r.PartnerId == partnerId);
        return await query
            .OrderByDescending(r => r.ReceivedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
