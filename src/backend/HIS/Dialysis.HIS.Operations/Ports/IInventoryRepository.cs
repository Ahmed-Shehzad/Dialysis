using Dialysis.HIS.Operations.Domain;

namespace Dialysis.HIS.Operations.Ports;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Update(InventoryItem item);
}
