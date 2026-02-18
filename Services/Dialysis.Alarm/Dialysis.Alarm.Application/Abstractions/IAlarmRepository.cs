using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Abstractions;

public interface IAlarmRepository : IRepository<AlarmDomain>
{
    Task<AlarmDomain?> GetByIdAsync(Ulid alarmId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlarmDomain>> GetByDeviceAndSessionAsync(DeviceId? deviceId, string? sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlarmDomain>> GetAlarmsAsync(DeviceId? deviceId, string? sessionId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default);
    /// <summary>Finds the most recent active/latched alarm for matching continue/end lifecycle messages.</summary>
    Task<AlarmDomain?> GetActiveBySourceAsync(DeviceId? deviceId, string? sessionId, string? sourceCode, CancellationToken cancellationToken = default);
}
