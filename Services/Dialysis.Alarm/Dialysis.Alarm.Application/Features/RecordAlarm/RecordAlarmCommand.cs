using Dialysis.Alarm.Application.Domain;

using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.RecordAlarm;

/// <summary>
/// Record an alarm from PCD-04 ORU^R40.
/// </summary>
public sealed record RecordAlarmCommand(AlarmInfo Alarm) : ICommand<RecordAlarmResponse>;
