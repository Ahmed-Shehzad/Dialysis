using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfInventoryRepository : IInventoryRepository
{
    private readonly HisDbContext _db;
    public EfInventoryRepository(HisDbContext db) => _db = db;
    public Task<InventoryItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public void Update(InventoryItem item) => _db.InventoryItems.Update(item);
}
