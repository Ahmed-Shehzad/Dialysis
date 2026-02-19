using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Infrastructure.Persistence;

public sealed class AlarmRepository : Repository<AlarmDomain>, IAlarmRepository
{
    private readonly AlarmDbContext _db;
    private readonly ITenantContext _tenant;

    public AlarmRepository(AlarmDbContext db, ITenantContext tenant) : base(db)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AlarmDomain?> GetActiveBySourceAsync(DeviceId? deviceId, string? sessionId, string? sourceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return null;

        IQueryable<AlarmDomain> query = _db.Alarms
            .Where(a => a.TenantId == _tenant.TenantId && a.SourceCode == sourceCode);
        if (deviceId is not null)
            query = query.Where(a => a.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(a => a.SessionId == sessionId);

        return await query
            .Where(a => a.AlarmState == AlarmState.Active || a.AlarmState == AlarmState.Latched)
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async override Task AddAsync(AlarmDomain entity, CancellationToken cancellationToken = default) =>
        _ = await _db.Alarms.AddAsync(entity, cancellationToken);

    public async override Task AddAsync(IEnumerable<AlarmDomain> entities, CancellationToken cancellationToken = default) =>
        await _db.Alarms.AddRangeAsync(entities, cancellationToken);

    public async override Task<IReadOnlyList<AlarmDomain>> GetManyAsync(
        Expression<Func<AlarmDomain, bool>> expression,
        Expression<Func<AlarmDomain, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AlarmDomain> query = _db.Alarms.AsNoTracking().Where(expression);

        if (orderByExpression != null)
            query = orderByDescending ? query.OrderByDescending(orderByExpression) : query.OrderBy(orderByExpression);

        return await query.ToListAsync(cancellationToken);
    }

    public async override Task<AlarmDomain?> GetAsync(Expression<Func<AlarmDomain, bool>> expression, CancellationToken cancellationToken = default) =>
        await _db.Alarms.FirstOrDefaultAsync(expression, cancellationToken);

    public override void Update(AlarmDomain entity) => _db.Update(entity);
    public override void Update(IEnumerable<AlarmDomain> entities) => _db.UpdateRange(entities);
    public override void Delete(AlarmDomain entity) => _db.Remove(entity);
    public override void Delete(IEnumerable<AlarmDomain> entities) => _db.RemoveRange(entities);
}
