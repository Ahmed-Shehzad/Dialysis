using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Medication.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfMedicationOrderRepository(HisDbContext db) : IMedicationOrderRepository
{
    public void Add(MedicationOrder order) => db.MedicationOrders.Add(order);

    public Task<MedicationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.MedicationOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
