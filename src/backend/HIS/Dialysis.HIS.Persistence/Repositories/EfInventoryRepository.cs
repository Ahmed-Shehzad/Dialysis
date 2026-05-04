using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfInventoryRepository(HisDbContext db) : IInventoryRepository
{
    public Task<InventoryItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public void Update(InventoryItem item) => db.InventoryItems.Update(item);
}
