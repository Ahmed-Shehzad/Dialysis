using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Domain;

public sealed class Alarm : AggregateRoot
{
    public string TenantId { get; private set; } = TenantContext.DefaultTenantId;
    public string? AlarmType { get; private set; }
    public string? SourceCode { get; private set; }
    public string? SourceLimits { get; private set; }
    public AlarmPriority? Priority { get; private set; }
    /// <summary>OBX-8 interpretation type: SP (system), ST (technical), SA (advisory).</summary>
    public string? InterpretationType { get; private set; }
    /// <summary>OBX-8 abnormality: L (low), H (high).</summary>
    public string? Abnormality { get; private set; }
    public EventPhase EventPhase { get; private set; }
    public AlarmState AlarmState { get; private set; }
    public ActivityState ActivityState { get; private set; }
    public DeviceId? DeviceId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    /// <summary>Absolute drift in seconds between MSH-7 and server UTC (IHE CT audit).</summary>
    public double? MessageTimeDriftSeconds { get; private set; }

    private Alarm() { }

    public static Alarm Raise(AlarmInfo info, string? tenantId = null)
    {
        var alarm = new Alarm
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantContext.DefaultTenantId : tenantId,
            AlarmType = info.AlarmType,
            SourceCode = info.SourceCode,
            SourceLimits = info.SourceLimits,
            Priority = info.Priority,
            InterpretationType = info.InterpretationType,
            Abnormality = info.Abnormality,
            EventPhase = info.EventPhase,
            AlarmState = info.AlarmState,
            ActivityState = info.ActivityState,
            DeviceId = info.DeviceId,
            SessionId = info.SessionId,
            OccurredAt = info.OccurredAt,
            MessageTimeDriftSeconds = info.MessageTimeDriftSeconds
        };

        alarm.ApplyEvent(new AlarmRaisedEvent(
            alarm.Id, info.AlarmType, info.EventPhase, info.AlarmState, info.DeviceId, info.SessionId, info.OccurredAt));
        return alarm;
    }

    public void Acknowledge()
    {
        AlarmState = AlarmState.Acknowledged;
        ApplyUpdateDateTime();
        ApplyEvent(new AlarmAcknowledgedEvent(Id));
    }

    public void Clear()
    {
        AlarmState = AlarmState.Cleared;
        ApplyUpdateDateTime();
        ApplyEvent(new AlarmClearedEvent(Id));
    }

    /// <summary>
    /// Updates alarm state for continue/end lifecycle messages (PCD-04).
    /// </summary>
    public void UpdateState(EventPhase eventPhase, AlarmState alarmState, ActivityState activityState, DateTimeOffset occurredAt)
    {
        EventPhase = eventPhase;
        AlarmState = alarmState;
        ActivityState = activityState;
        OccurredAt = occurredAt;
        ApplyUpdateDateTime();
    }
}
