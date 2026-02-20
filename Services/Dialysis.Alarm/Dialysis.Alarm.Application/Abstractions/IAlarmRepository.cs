using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Abstractions;

public interface IAlarmRepository : IRepository<AlarmDomain>
{
    /// <summary>Finds the most recent active/latched alarm for matching continue/end lifecycle messages.</summary>
    Task<AlarmDomain?> GetActiveBySourceAsync(DeviceId? deviceId, SessionId? sessionId, string? sourceCode, CancellationToken cancellationToken = default);

    /// <summary>Gets recent alarms (not Cleared/Acknowledged) within the specified window, for escalation evaluation. Optionally includes a pending alarm from the change tracker (e.g. the one being saved).</summary>
    Task<IReadOnlyList<AlarmDomain>> GetRecentActiveAlarmsForEscalationAsync(DeviceId? deviceId, SessionId? sessionId, TimeSpan withinLast, Ulid? includeAlarmIdFromTracker, CancellationToken cancellationToken = default);
}
