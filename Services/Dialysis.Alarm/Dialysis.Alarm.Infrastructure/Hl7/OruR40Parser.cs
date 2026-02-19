using System.Globalization;

using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Domain.Hl7;
using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 ORU^R40 (PCD-04) alarm messages.
/// Each alarm is exactly 5 OBX segments in order: (1) alarm type, (2) source/limits, (3) event phase, (4) alarm state, (5) activity state.
/// </summary>
public sealed class OruR40Parser : IOruR40MessageParser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    private static readonly string EventPhaseCode = "68481";
    private static readonly string AlarmStateCode = "68482";
    private static readonly string ActivityStateCode = "68483";

    public OruR40ParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        string? deviceId = ParseDeviceId(segments);
        string? sessionId = ParseSessionId(segments);
        DateTimeOffset? messageTimestamp = ParseMessageTimestamp(segments);

        List<AlarmInfo> alarms = ParseAlarmGroups(segments, deviceId, sessionId, messageTimestamp);

        return new OruR40ParseResult(deviceId, sessionId, alarms);
    }

    private static string? ParseDeviceId(string[] segments)
    {
        string? msh = FindFirstSegment(segments, "MSH");
        if (msh is null) return null;
        return ExtractField(msh, 2) ?? ExtractField(msh, 3);
    }

    private static string? ParseSessionId(string[] segments)
    {
        string? obr = FindFirstSegment(segments, "OBR");
        if (obr is null) return null;
        string? obr3 = ExtractField(obr, 3);
        if (string.IsNullOrEmpty(obr3)) return null;
        string[] parts = obr3.Split(ComponentSeparator);
        return parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) ? parts[0] : null;
    }

    private static DateTimeOffset? ParseMessageTimestamp(string[] segments)
    {
        string? msh = FindFirstSegment(segments, "MSH");
        if (msh is null) return null;
        string? raw = ExtractField(msh, 6);
        return ParseHl7DateTime(raw);
    }

    private static List<AlarmInfo> ParseAlarmGroups(string[] segments, string? deviceId, string? sessionId, DateTimeOffset? messageTimestamp)
    {
        string[] obxList = segments.Where(s => s.StartsWith("OBX", StringComparison.Ordinal)).ToArray();
        var alarms = new List<AlarmInfo>();

        for (int i = 0; i + 4 < obxList.Length; i += 5)
        {
            string obx1 = obxList[i];
            if (!TryParseAlarmGroup(obxList, i, out AlarmCreateParams? createParams) || createParams is null)
                continue;

            DateTimeOffset occurredAt = GetEffectiveTime(obx1, messageTimestamp);
            DeviceId? deviceIdVo = string.IsNullOrWhiteSpace(deviceId) ? null : (DeviceId?)new DeviceId(deviceId);
            BuildingBlocks.ValueObjects.SessionId? sessionIdVo = string.IsNullOrWhiteSpace(sessionId) ? null : new BuildingBlocks.ValueObjects.SessionId(sessionId);

            AlarmCreateParams finalParams = createParams with { OccurredAt = occurredAt, DeviceId = deviceIdVo, SessionId = sessionIdVo };
            alarms.Add(AlarmInfo.Create(finalParams));
        }

        return alarms;
    }

    private static bool TryParseAlarmGroup(string[] obxSegments, int startIndex, out AlarmCreateParams? createParams)
    {
        createParams = null;

        string obx1 = obxSegments[startIndex];
        string obx2 = obxSegments[startIndex + 1];
        string obx3 = obxSegments[startIndex + 2];
        string obx4 = obxSegments[startIndex + 3];
        string obx5 = obxSegments[startIndex + 4];

        string? obx1Field3 = ExtractField(obx1, 3);
        string eventType = ExtractCodedElementCode(obx1Field3);
        string? sourceIdentifier = ExtractField(obx1, 5);
        string sourceCode = ExtractCodedElementCode(sourceIdentifier ?? string.Empty);

        string? sourceValue = ExtractField(obx2, 5);
        string? sourceUnit = ExtractField(obx2, 6);
        string? sourceRange = ExtractField(obx2, 7);
        string sourceLimits = BuildSourceLimits(sourceValue ?? string.Empty, sourceUnit ?? string.Empty, sourceRange ?? string.Empty);

        string? eventPhaseRaw = ExtractField(obx3, 5);
        string? alarmStateRaw = ExtractField(obx4, 5);
        string? activityStateRaw = ExtractField(obx5, 5);

        if (string.IsNullOrEmpty(eventPhaseRaw) || string.IsNullOrEmpty(alarmStateRaw) || string.IsNullOrEmpty(activityStateRaw))
            return false;
        if (!IsExpectedObxStructure(obx3, obx4, obx5, eventPhaseRaw, alarmStateRaw, activityStateRaw))
            return false;

        var eventPhase = new EventPhase(eventPhaseRaw);
        var alarmState = new AlarmState(alarmStateRaw);
        var activityState = new ActivityState(activityStateRaw);
        var state = new AlarmStateDescriptor(eventPhase, alarmState, activityState);

        (AlarmPriority? priority, string? interpretationType, string? abnormality) = ParseObx8InterpretationCodes(obx1);
        string? displayName = MandatoryAlarmCatalog.GetDisplayName(sourceCode, eventType);
        string alarmType = displayName ?? eventType;

        createParams = new AlarmCreateParams(alarmType, sourceCode, sourceLimits, state, priority, interpretationType, abnormality, displayName, null, null, DateTimeOffset.UtcNow);
        return true;
    }

    private static bool IsExpectedObxStructure(string obx3, string obx4, string obx5, string eventPhase, string alarmState, string activityState)
    {
        string code3 = ExtractCodedElementPrimaryId(ExtractField(obx3, 3) ?? string.Empty);
        string code4 = ExtractCodedElementPrimaryId(ExtractField(obx4, 3) ?? string.Empty);
        string code5 = ExtractCodedElementPrimaryId(ExtractField(obx5, 3) ?? string.Empty);

        return code3 == EventPhaseCode && code4 == AlarmStateCode && code5 == ActivityStateCode
               && !string.IsNullOrEmpty(eventPhase) && !string.IsNullOrEmpty(alarmState) && !string.IsNullOrEmpty(activityState);
    }

    private static string BuildSourceLimits(string value, string unit, string range)
    {
        if (string.IsNullOrEmpty(value)) return range ?? string.Empty;
        string withUnit = string.IsNullOrEmpty(unit) ? value : $"{value} {unit}";
        return string.IsNullOrEmpty(range) ? withUnit : $"{withUnit} ({range})";
    }

    /// <summary>
    /// Parses OBX-8 interpretation codes: priority (PH/PM/PL/PI/PN/PU), type (SP/ST/SA), abnormality (L/H).
    /// </summary>
    private static (AlarmPriority? priority, string? interpretationType, string? abnormality) ParseObx8InterpretationCodes(string obxSegment)
    {
        string? obx8 = ExtractField(obxSegment, 8);
        if (string.IsNullOrEmpty(obx8))
            return (null, null, null);

        AlarmPriority? priority = null;
        string? interpretationType = null;
        string? abnormality = null;

        string[] codes = obx8.Split('~');
        foreach (string code in codes)
        {
            string c = code.Trim();
            if (c is "PH" or "PM" or "PL" or "PI" or "PN" or "PU")
                priority = new AlarmPriority(c);
            else if (c is "SP" or "ST" or "SA")
                interpretationType = c;
            else if (c is "L" or "H")
                abnormality = c;
        }

        return (priority, interpretationType, abnormality);
    }

    private static DateTimeOffset GetEffectiveTime(string obxSegment, DateTimeOffset? fallback)
    {
        string? raw = ExtractField(obxSegment, 14);
        return ParseHl7DateTime(raw) ?? fallback ?? DateTimeOffset.UtcNow;
    }

    private static string ExtractCodedElementCode(string? ce)
    {
        if (string.IsNullOrEmpty(ce)) return string.Empty;
        string[] parts = ce.Split(ComponentSeparator);
        return parts.Length >= 2 ? parts[1] : parts[0];
    }

    /// <summary>
    /// Extracts the numeric code for structure validation. Handles both formats:
    /// 68481^MDC_ATTR_EVT_PHASE^MDC (code first) and MDC_ATTR_EVT_PHASE^68481^MDC (identifier first).
    /// </summary>
    private static string ExtractCodedElementPrimaryId(string? ce)
    {
        if (string.IsNullOrEmpty(ce)) return string.Empty;
        string[] parts = ce.Split(ComponentSeparator);
        foreach (string p in parts)
            if (p.Length > 0 && p.All(char.IsDigit))
                return p;

        return parts[0];
    }

    private static DateTimeOffset? ParseHl7DateTime(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        string[] formats = ["yyyyMMddHHmmss.ffffzzz", "yyyyMMddHHmmsszzz", "yyyyMMddHHmmss.ffff", "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd"];
        string normalized = NormalizeHl7Timezone(raw);

        return DateTimeOffset.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dt)
            ? dt
            : null;
    }

    private static string NormalizeHl7Timezone(string raw)
    {
        int idx = raw.LastIndexOf('+');
        if (idx < 0) idx = raw.LastIndexOf('-');
        if (idx >= 0 && idx + 5 <= raw.Length && char.IsDigit(raw[idx + 1]))
        {
            string tz = raw[idx..];
            if (tz.Length == 5 && char.IsDigit(tz[1]) && char.IsDigit(tz[2]) && char.IsDigit(tz[3]) && char.IsDigit(tz[4]))
                return raw[..idx] + tz[..3] + ":" + tz[3..];
        }
        return raw;
    }

    private static string[] SplitSegments(string hl7Message)
    {
        string normalized = hl7Message.Replace("\r\n", "\r").Replace("\n", "\r");
        return normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string? FindFirstSegment(string[] segments, string segmentType)
    {
        foreach (string seg in segments)
            if (seg.StartsWith(segmentType + FieldSeparator, StringComparison.Ordinal) || seg.Equals(segmentType, StringComparison.Ordinal))
                return seg;

        return null;
    }

    private static string? ExtractField(string segment, int fieldIndex)
    {
        string[] fields = segment.Split(FieldSeparator);
        if (fieldIndex < 0 || fieldIndex >= fields.Length) return null;
        return fields[fieldIndex].Trim();
    }
}
