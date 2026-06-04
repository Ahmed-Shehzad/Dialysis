using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Medication.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfMedicationOrderRepository : IMedicationOrderRepository
{
    private readonly HisDbContext _db;
    public EfMedicationOrderRepository(HisDbContext db) => _db = db;
    public void Add(MedicationOrder order) => _db.MedicationOrders.Add(order);

    public Task<MedicationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.MedicationOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
