using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.GetAlarms;

/// <summary>
/// FHIR search params: _id, deviceId, sessionId, date (from/to).
/// </summary>
public sealed record GetAlarmsQuery(
    string? Id = null,
    string? DeviceId = null,
    string? SessionId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null) : IQuery<GetAlarmsResponse>;
