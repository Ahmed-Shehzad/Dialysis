using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;

using Microsoft.EntityFrameworkCore;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Infrastructure.Persistence;

public sealed class DeviceRepository : Repository<DeviceDomain>, IDeviceRepository
{
    private readonly DeviceDbContext _db;
    private readonly ITenantContext _tenant;

    public DeviceRepository(DeviceDbContext db, ITenantContext tenant) : base(db)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DeviceDomain?> GetByDeviceEui64Async(string deviceEui64, CancellationToken cancellationToken = default)
    {
        return await _db.Devices
            .FirstOrDefaultAsync(d => d.TenantId == _tenant.TenantId && d.DeviceEui64 == deviceEui64, cancellationToken);
    }

    public override async Task AddAsync(DeviceDomain entity, CancellationToken cancellationToken = default) =>
        await _db.Devices.AddAsync(entity, cancellationToken);

    public override async Task AddAsync(IEnumerable<DeviceDomain> entities, CancellationToken cancellationToken = default) =>
        await _db.Devices.AddRangeAsync(entities, cancellationToken);

    public override async Task<IReadOnlyList<DeviceDomain>> GetManyAsync(
        Expression<Func<DeviceDomain, bool>> expression,
        Expression<Func<DeviceDomain, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DeviceDomain> query = _db.Devices.AsNoTracking().Where(expression);
        if (orderByExpression != null)
            query = orderByDescending ? query.OrderByDescending(orderByExpression) : query.OrderBy(orderByExpression);
        return await query.ToListAsync(cancellationToken);
    }

    public override async Task<DeviceDomain?> GetAsync(Expression<Func<DeviceDomain, bool>> expression, CancellationToken cancellationToken = default) =>
        await _db.Devices.FirstOrDefaultAsync(expression, cancellationToken);

    public override void Update(DeviceDomain entity) => _db.Update(entity);
    public override void Update(IEnumerable<DeviceDomain> entities) => _db.UpdateRange(entities);
    public override void Delete(DeviceDomain entity) => _db.Remove(entity);
    public override void Delete(IEnumerable<DeviceDomain> entities) => _db.RemoveRange(entities);
}
