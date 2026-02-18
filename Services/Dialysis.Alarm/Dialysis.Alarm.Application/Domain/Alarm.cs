using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Domain;

public sealed class Alarm : AggregateRoot
{
    public string? AlarmType { get; private set; }
    public string? SourceLimits { get; private set; }
    public EventPhase EventPhase { get; private set; }
    public AlarmState AlarmState { get; private set; }
    public ActivityState ActivityState { get; private set; }
    public DeviceId? DeviceId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private Alarm() { }

    public static Alarm Raise(AlarmInfo info)
    {
        var alarm = new Alarm
        {
            AlarmType = info.AlarmType,
            SourceLimits = info.SourceLimits,
            EventPhase = info.EventPhase,
            AlarmState = info.AlarmState,
            ActivityState = info.ActivityState,
            DeviceId = info.DeviceId,
            SessionId = info.SessionId,
            OccurredAt = info.OccurredAt
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
}
