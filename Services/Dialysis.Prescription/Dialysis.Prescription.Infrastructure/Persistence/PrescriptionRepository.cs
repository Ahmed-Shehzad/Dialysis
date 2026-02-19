using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;

using Microsoft.EntityFrameworkCore;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Infrastructure.Persistence;

public sealed class PrescriptionRepository : Repository<PrescriptionEntity>, IPrescriptionRepository
{
    private readonly PrescriptionDbContext _db;
    private readonly ITenantContext _tenant;

    public PrescriptionRepository(PrescriptionDbContext db, ITenantContext tenant) : base(db)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PrescriptionEntity?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.TenantId == new TenantId(_tenant.TenantId) && p.OrderId == orderId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PrescriptionEntity?> GetLatestByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default)
    {
        return await _db.Prescriptions
            .Where(p => p.TenantId == new TenantId(_tenant.TenantId) && p.PatientMrn == mrn)
            .OrderByDescending(p => p.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async override Task AddAsync(PrescriptionEntity entity, CancellationToken cancellationToken = default) =>
        _ = await _db.Prescriptions.AddAsync(entity, cancellationToken);

    public async override Task AddAsync(IEnumerable<PrescriptionEntity> entities, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions.AddRangeAsync(entities, cancellationToken);

    public async override Task<IReadOnlyList<PrescriptionEntity>> GetManyAsync(
        Expression<Func<PrescriptionEntity, bool>> expression,
        Expression<Func<PrescriptionEntity, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PrescriptionEntity> query = _db.Prescriptions.AsNoTracking().Where(expression);
        if (orderByExpression != null)
            query = orderByDescending ? query.OrderByDescending(orderByExpression) : query.OrderBy(orderByExpression);
        return await query.ToListAsync(cancellationToken);
    }

    public async override Task<PrescriptionEntity?> GetAsync(Expression<Func<PrescriptionEntity, bool>> expression, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions.FirstOrDefaultAsync(expression, cancellationToken);

    public override void Update(PrescriptionEntity entity) => _db.Update(entity);
    public override void Update(IEnumerable<PrescriptionEntity> entities) => _db.UpdateRange(entities);
    public override void Delete(PrescriptionEntity entity) => _db.Remove(entity);
    public override void Delete(IEnumerable<PrescriptionEntity> entities) => _db.RemoveRange(entities);
}
