using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfReceivedResourceStore(HieDbContext db) : IReceivedResourceStore
{
    public async Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default)
    {
        var existing = await db.ReceivedResources
            .FirstOrDefaultAsync(
                r => r.PartnerId == resource.PartnerId
                    && r.ResourceType == resource.ResourceType
                    && r.LogicalId == resource.LogicalId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            await db.ReceivedResources.AddAsync(resource, cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
