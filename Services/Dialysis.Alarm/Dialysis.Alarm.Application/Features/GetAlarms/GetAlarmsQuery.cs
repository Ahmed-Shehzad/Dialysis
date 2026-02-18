using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.GetAlarms;

public sealed record GetAlarmsQuery(
    string? DeviceId,
    string? SessionId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc) : IQuery<GetAlarmsResponse>;
