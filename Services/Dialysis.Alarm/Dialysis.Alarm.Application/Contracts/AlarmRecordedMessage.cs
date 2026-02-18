using Transponder.Abstractions;

namespace Dialysis.Alarm.Application.Contracts;

/// <summary>
/// Transponder message for real-time alarm broadcast via SignalR.
/// </summary>
public sealed record AlarmRecordedMessage(
    string AlarmId,
    string? AlarmType,
    string EventPhase,
    string AlarmState,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt) : IMessage;
