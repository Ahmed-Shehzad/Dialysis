using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Abstractions;

public interface IAlarmRepository : IRepository<AlarmDomain>
{
    /// <summary>Finds the most recent active/latched alarm for matching continue/end lifecycle messages.</summary>
    Task<AlarmDomain?> GetActiveBySourceAsync(DeviceId? deviceId, string? sessionId, string? sourceCode, CancellationToken cancellationToken = default);
}
