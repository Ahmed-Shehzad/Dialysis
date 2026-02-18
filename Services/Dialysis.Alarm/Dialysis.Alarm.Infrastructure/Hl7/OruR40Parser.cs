using System.Globalization;

using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.ValueObjects;

using Efferent.HL7.V2;

namespace Dialysis.Alarm.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 ORU^R40 (PCD-04) alarm messages.
/// OBX structure: (1) alarm type, (2) source/limits, (3) event phase, (4) alarm state, (5) activity state.
/// </summary>
public sealed class OruR40Parser : IOruR40MessageParser
{
    public OruR40ParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        var msg = new Message(hl7Message);
        if (!msg.ParseMessage(bypassValidation: true))
            throw new ArgumentException("Invalid HL7 ORU^R40 message.", nameof(hl7Message));

        string deviceId = SafeGetValue(msg, "MSH", 3);
        string? sessionId = GetSessionId(msg);

        var alarms = new List<AlarmInfo>();
        int obxIndex = 1;
        while (true)
        {
            string alarmType = SafeGetValue(msg, "OBX", obxIndex, 3);
            if (string.IsNullOrEmpty(alarmType))
                break;

            string sourceLimits = SafeGetValue(msg, "OBX", obxIndex, 5);
            string eventPhase = SafeGetValue(msg, "OBX", obxIndex, 6) ?? "start";
            string alarmState = SafeGetValue(msg, "OBX", obxIndex, 7) ?? "active";
            string activityState = SafeGetValue(msg, "OBX", obxIndex, 8) ?? "enabled";
            string effectiveTimeStr = SafeGetValue(msg, "OBX", obxIndex, 14);
            DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
            if (DateTimeOffset.TryParse(effectiveTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dt))
                occurredAt = dt;

            var state = new AlarmStateDescriptor(
                new EventPhase(eventPhase),
                new AlarmState(alarmState),
                new ActivityState(activityState));
            DeviceId? deviceIdOrNull = string.IsNullOrWhiteSpace(deviceId) ? null : (DeviceId?)new DeviceId(deviceId);
            alarms.Add(AlarmInfo.Create(alarmType, sourceLimits, state, deviceIdOrNull, sessionId, occurredAt));
            obxIndex++;
        }

        return new OruR40ParseResult(deviceId, sessionId, alarms);
    }

    private static string? GetSessionId(Message msg)
    {
        string obr3 = SafeGetValue(msg, "OBR", 1, 3);
        if (!string.IsNullOrEmpty(obr3))
        {
            string[] parts = obr3.Split('^');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                return parts[0];
        }
        return SafeGetValue(msg, "MSH", 10);
    }

    private static string SafeGetValue(Message msg, string segment, int fieldIndex)
    {
        try
        {
            return msg.GetValue($"{segment}.{fieldIndex}") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SafeGetValue(Message msg, string segment, int occurrence, int fieldIndex)
    {
        try
        {
            return msg.GetValue($"{segment}({occurrence}).{fieldIndex}") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
