using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Groups event phase, alarm state, and activity state for alarm creation.
/// </summary>
public sealed record AlarmStateDescriptor(EventPhase EventPhase, AlarmState AlarmState, ActivityState ActivityState);

public interface IAlarmRepository
{
    Task<AlarmDomain> AddAsync(AlarmDomain alarm, CancellationToken cancellationToken = default);
    Task<AlarmDomain?> GetByIdAsync(Ulid alarmId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlarmDomain>> GetByDeviceAndSessionAsync(DeviceId? deviceId, string? sessionId, CancellationToken cancellationToken = default);
    Task SaveAsync(AlarmDomain alarm, CancellationToken cancellationToken = default);
}

public sealed record AlarmInfo(
    Ulid? Id,
    string? AlarmType,
    string? SourceLimits,
    EventPhase EventPhase,
    AlarmState AlarmState,
    ActivityState ActivityState,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt)
{
    public static AlarmInfo Create(
        string? alarmType,
        string? sourceLimits,
        AlarmStateDescriptor state,
        DeviceId? deviceId,
        string? sessionId,
        DateTimeOffset occurredAt) =>
        new(null, alarmType, sourceLimits, state.EventPhase, state.AlarmState, state.ActivityState, deviceId, sessionId, occurredAt);
}
