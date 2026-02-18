using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;

using Microsoft.EntityFrameworkCore;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Infrastructure.Persistence;

public sealed class AlarmRepository : Repository<AlarmDomain>, IAlarmRepository
{
    private readonly AlarmDbContext _db;

    public AlarmRepository(AlarmDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<AlarmDomain?> GetByIdAsync(Ulid alarmId, CancellationToken cancellationToken = default)
    {
        return await _db.Alarms
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alarmId, cancellationToken);
    }

    public async Task<IReadOnlyList<AlarmDomain>> GetByDeviceAndSessionAsync(DeviceId? deviceId, string? sessionId, CancellationToken cancellationToken = default)
    {
        IQueryable<AlarmDomain> query = _db.Alarms.AsNoTracking();
        if (deviceId is not null)
            query = query.Where(a => a.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(a => a.SessionId == sessionId);
        return await query.OrderByDescending(a => a.OccurredAt).ToListAsync(cancellationToken);
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
