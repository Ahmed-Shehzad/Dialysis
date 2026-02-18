using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Groups event phase, alarm state, and activity state for alarm creation.
/// </summary>
public sealed record AlarmStateDescriptor(EventPhase EventPhase, AlarmState AlarmState, ActivityState ActivityState);
