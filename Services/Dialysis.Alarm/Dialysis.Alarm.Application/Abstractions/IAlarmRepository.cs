using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Abstractions;

public interface IAlarmRepository : IRepository<AlarmDomain>
{
    Task<AlarmDomain?> GetByIdAsync(Ulid alarmId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlarmDomain>> GetByDeviceAndSessionAsync(DeviceId? deviceId, string? sessionId, CancellationToken cancellationToken = default);
}
