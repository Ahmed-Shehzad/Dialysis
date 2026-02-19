using System.Globalization;
using System.Text.RegularExpressions;

using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.Hl7;
using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 ORU^R01 (PCD-01) messages following the IEEE 11073 containment model.
/// Handles MDS → VMD → Channel → Metric hierarchy via OBX-4 dotted sub-IDs.
/// </summary>
public sealed partial class OruR01Parser : IOruMessageParser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    public OruParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        ParseMsh(segments, out string? sendingApp, out string? sendingFacility, out DateTimeOffset? messageTimestamp, out string? messageControlId);
        string? patientMrn = ParsePid(segments);
        ParseObr(segments, out string? sessionId, out string? eventPhaseStr, out string? therapyId, out string? deviceEui64);

        sessionId ??= messageControlId ?? Ulid.NewUlid().ToString();

        EventPhase? phase = !string.IsNullOrEmpty(eventPhaseStr) ? new EventPhase(eventPhaseStr) : null;

        string? deviceId = sendingApp;
        string? eui64 = deviceEui64 ?? sendingApp;
        List<ObservationInfo> observations = ParseAllObx(segments);

        return new OruParseResult(
            new SessionId(sessionId),
            !string.IsNullOrWhiteSpace(patientMrn) ? new MedicalRecordNumber(patientMrn) : null,
            !string.IsNullOrWhiteSpace(deviceId) ? new DeviceId(deviceId) : null,
            phase,
            sendingApp,
            sendingFacility,
            messageTimestamp,
            observations,
            eui64,
            therapyId ?? sessionId);
    }

    // ─── MSH Parsing ─────────────────────────────────────────────────────────

    private static void ParseMsh(
        string[] segments,
        out string? sendingApp,
        out string? sendingFacility,
        out DateTimeOffset? messageTimestamp,
        out string? messageControlId)
    {
        sendingApp = null;
        sendingFacility = null;
        messageTimestamp = null;
        messageControlId = null;

        string? msh = FindFirstSegment(segments, "MSH");
        if (msh is null) return;

        string[] fields = msh.Split(FieldSeparator);

        sendingApp = SafeField(fields, 2);
        sendingFacility = SafeField(fields, 3);
        messageTimestamp = ParseHl7DateTime(SafeField(fields, 6));
        messageControlId = SafeField(fields, 9);
    }

    // ─── PID Parsing ─────────────────────────────────────────────────────────

    private static string? ParsePid(string[] segments)
    {
        string? pid = FindFirstSegment(segments, "PID");
        if (pid is null) return null;

        string[] fields = pid.Split(FieldSeparator);
        string raw = SafeField(fields, 3);
        if (string.IsNullOrEmpty(raw)) return null;

        string[] components = raw.Split(ComponentSeparator);
        return components.Length > 0 && !string.IsNullOrEmpty(components[0])
            ? components[0]
            : null;
    }

    // ─── OBR Parsing ─────────────────────────────────────────────────────────

    private static void ParseObr(string[] segments, out string? sessionId, out string? eventPhase, out string? therapyId, out string? deviceEui64)
    {
        sessionId = null;
        eventPhase = null;
        therapyId = null;
        deviceEui64 = null;

        string? obr = FindFirstSegment(segments, "OBR");
        if (obr is null) return;

        string[] fields = obr.Split(FieldSeparator);

        string fillerOrder = SafeField(fields, 3);
        if (!string.IsNullOrEmpty(fillerOrder))
        {
            string[] parts = fillerOrder.Split(ComponentSeparator);
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                sessionId = parts[0];
            if (parts.Length > 0)
                therapyId = fillerOrder;
            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                deviceEui64 = parts[2];
        }

        string obr12 = SafeField(fields, 12);
        if (!string.IsNullOrEmpty(obr12))
            eventPhase = obr12.Split(ComponentSeparator)[0];
    }

    // ─── OBX Parsing (Full IEEE 11073 Hierarchy) ─────────────────────────────

    private static List<ObservationInfo> ParseAllObx(string[] segments)
    {
        var observations = new List<ObservationInfo>();

        foreach (string segment in segments)
        {
            if (!segment.StartsWith("OBX", StringComparison.Ordinal))
                continue;

            ObservationInfo? obs = ParseSingleObx(segment);
            if (obs is not null)
                observations.Add(obs);
        }

        return observations;
    }

    private static ObservationInfo? ParseSingleObx(string segment)
    {
        string[] fields = segment.Split(FieldSeparator);
        if (fields.Length < 6) return null;

        string obx3Raw = SafeField(fields, 3);
        if (string.IsNullOrEmpty(obx3Raw)) return null;

        string code = ParseCodedElement(obx3Raw);
        if (string.IsNullOrEmpty(code)) return null;

        string? value = SafeField(fields, 5);
        string? unit = ExtractUcumUnit(SafeField(fields, 6));
        string? subId = SafeField(fields, 4);
        string? referenceRange = SafeField(fields, 7);
        string? resultStatus = SafeField(fields, 11);
        DateTimeOffset? effectiveTime = ParseHl7DateTime(SafeField(fields, 14));
        string? provenance = SafeField(fields, 17);
        string? equipmentId = SafeField(fields, 18);

        var path = ContainmentPath.TryParse(subId);
        ContainmentLevel? level = path?.Level ?? (MdcCodeCatalog.TryGet(code, out MdcCodeDescriptor d) ? d.Level : null);

        ObservationStatus? status = !string.IsNullOrEmpty(resultStatus)
            ? new ObservationStatus(resultStatus)
            : null;

        return new ObservationInfo(
            new ObservationCode(code),
            value,
            unit,
            subId,
            referenceRange,
            status,
            effectiveTime,
            provenance,
            equipmentId,
            level);
    }

    // ─── HL7 Coded Element (CE) Parsing ──────────────────────────────────────

    private static string ParseCodedElement(string raw)
    {
        string[] parts = raw.Split(ComponentSeparator);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>
    /// Extracts the unit code from OBX-6 (CE format: code^text^codingSystem).
    /// </summary>
    private static string? ExtractUcumUnit(string? obx6)
    {
        if (string.IsNullOrEmpty(obx6)) return null;
        string[] parts = obx6.Split(ComponentSeparator);
        return parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) ? parts[0] : null;
    }

    // ─── HL7 DateTime Parsing ────────────────────────────────────────────────

    private static DateTimeOffset? ParseHl7DateTime(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        string[] formats =
        [
            "yyyyMMddHHmmss.ffffzzz",
            "yyyyMMddHHmmsszzz",
            "yyyyMMddHHmmss.ffff",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMdd"
        ];

        string normalized = NormalizeHl7Timezone(raw);

        return DateTimeOffset.TryParseExact(
            normalized,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out DateTimeOffset dt)
            ? dt
            : null;
    }

    /// <summary>
    /// HL7 uses +HHMM timezone offsets (no colon). .NET expects +HH:MM for zzz patterns.
    /// </summary>
    private static string NormalizeHl7Timezone(string raw)
    {
        Match match = Hl7TimezoneRegex().Match(raw);
        if (match.Success)
        {
            string tzOffset = match.Groups[1].Value;
            string sign = tzOffset[..1];
            string hhmm = tzOffset[1..];
            if (hhmm.Length == 4)
                return raw[..match.Index] + sign + hhmm[..2] + ":" + hhmm[2..];
        }

        return raw;
    }

    [GeneratedRegex(@"([+\-]\d{4})$")]
    private static partial Regex Hl7TimezoneRegex();

    // ─── Segment Utilities ───────────────────────────────────────────────────

    private static string[] SplitSegments(string hl7Message)
    {
        string normalized = hl7Message.Replace("\r\n", "\r").Replace("\n", "\r");
        return normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string? FindFirstSegment(string[] segments, string segmentType) => segments.FirstOrDefault(seg => seg.StartsWith(segmentType + FieldSeparator, StringComparison.Ordinal) || seg.Equals(segmentType, StringComparison.Ordinal));

    private static string SafeField(string[] fields, int index)
    {
        if (fields[0].StartsWith("MSH", StringComparison.Ordinal))
            index++;

        return index >= 0 && index < fields.Length ? fields[index].Trim() : string.Empty;
    }
}
