using BuildingBlocks.ValueObjects;

namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Read-only store for Alarm queries. Used by query handlers instead of the write repository.
/// </summary>
public interface IAlarmReadStore
{
    Task<AlarmReadDto?> GetByIdAsync(string tenantId, string alarmId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlarmReadDto>> GetAlarmsAsync(string tenantId, DeviceId? deviceId, string? sessionId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for alarm query results.
/// </summary>
public sealed record AlarmReadDto(
    string Id,
    string? AlarmType,
    string? SourceCode,
    string? SourceLimits,
    string? Priority,
    string? InterpretationType,
    string? Abnormality,
    string EventPhase,
    string AlarmState,
    string ActivityState,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt);
