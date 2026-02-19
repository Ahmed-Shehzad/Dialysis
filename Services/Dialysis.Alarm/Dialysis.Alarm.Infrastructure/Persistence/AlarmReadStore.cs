using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Infrastructure.Persistence;
using Dialysis.Alarm.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alarm.Infrastructure;

public sealed class AlarmReadStore : IAlarmReadStore
{
    private readonly AlarmReadDbContext _db;

    public AlarmReadStore(AlarmReadDbContext db)
    {
        _db = db;
    }

    public async Task<AlarmReadDto?> GetByIdAsync(string tenantId, string alarmId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alarmId)) return null;
        AlarmReadModel? m = await _db.Alarms
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alarmId, cancellationToken);
        return m is null ? null : ToDto(m);
    }

    public async Task<IReadOnlyList<AlarmReadDto>> GetAlarmsAsync(string tenantId, DeviceId? deviceId, string? sessionId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default)
    {
        IQueryable<AlarmReadModel> query = _db.Alarms
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);
        if (deviceId is not null)
            query = query.Where(a => a.DeviceId == deviceId.Value);
        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(a => a.SessionId == sessionId);
        if (fromUtc is not null)
            query = query.Where(a => a.OccurredAt >= fromUtc.Value);
        if (toUtc is not null)
            query = query.Where(a => a.OccurredAt <= toUtc.Value);

        List<AlarmReadModel> list = await query
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    private static AlarmReadDto ToDto(AlarmReadModel m) =>
        new(
            m.Id,
            m.AlarmType,
            m.SourceCode,
            m.SourceLimits,
            m.Priority,
            m.InterpretationType,
            m.Abnormality,
            m.EventPhase,
            m.AlarmState,
            m.ActivityState,
            m.DeviceId,
            m.SessionId,
            m.OccurredAt);
}
