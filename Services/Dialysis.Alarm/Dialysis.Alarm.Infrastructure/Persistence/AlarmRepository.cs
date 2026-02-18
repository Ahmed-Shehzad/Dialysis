using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;

using Microsoft.EntityFrameworkCore;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Infrastructure.Persistence;

public sealed class AlarmRepository : IAlarmRepository
{
    private readonly AlarmDbContext _db;

    public AlarmRepository(AlarmDbContext db)
    {
        _db = db;
    }

    public async Task<AlarmDomain> AddAsync(AlarmDomain alarm, CancellationToken cancellationToken = default)
    {
        _ = _db.Alarms.Add(alarm);
        _ = await _db.SaveChangesAsync(cancellationToken);
        return alarm;
    }

    public async Task<AlarmDomain?> GetByIdAsync(Ulid alarmId, CancellationToken cancellationToken = default)
    {
        return await _db.Alarms
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alarmId, cancellationToken);
    }

    public async Task<IReadOnlyList<AlarmDomain>> GetByDeviceAndSessionAsync(DeviceId? deviceId, string? sessionId, CancellationToken cancellationToken = default)
    {
        var query = _db.Alarms.AsNoTracking();
        if (deviceId is not null)
            query = query.Where(a => a.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(a => a.SessionId == sessionId);
        return await query.OrderByDescending(a => a.OccurredAt).ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(AlarmDomain alarm, CancellationToken cancellationToken = default)
    {
        _ = await _db.SaveChangesAsync(cancellationToken);
    }
}
