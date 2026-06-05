using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Lab.Persistence.Repositories;

/// <summary>EF-backed <see cref="ILabOrderRepository"/> on <see cref="LabDbContext"/>.</summary>
public sealed class EfLabOrderRepository : ILabOrderRepository
{
    private readonly LabDbContext _db;
    public EfLabOrderRepository(LabDbContext db) => _db = db;

    public void Add(LabOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _db.LabOrders.Add(order);
    }

    public Task<LabOrder?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _db.LabOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<LabOrder?> FindByPlacerOrderNumberAsync(string placerOrderNumber, CancellationToken cancellationToken) =>
        _db.LabOrders.FirstOrDefaultAsync(o => o.PlacerOrderNumber == placerOrderNumber, cancellationToken);

    public async Task<IReadOnlyList<LabOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.LabOrders
            .AsNoTracking()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.PlacedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }
}
