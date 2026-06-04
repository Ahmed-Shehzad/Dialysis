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
        var existing = await _db.ReceivedResources
            .FirstOrDefaultAsync(
                r => r.PartnerId == resource.PartnerId
                    && r.ResourceType == resource.ResourceType
                    && r.LogicalId == resource.LogicalId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            await _db.ReceivedResources.AddAsync(resource, cancellationToken).ConfigureAwait(false);
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
