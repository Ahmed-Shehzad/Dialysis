using System.Globalization;

using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 RSP^K22 prescription response messages.
/// Structure: MSH, MSA, QAK, QPD, ORC, {OBX}.
/// </summary>
public sealed class RspK22Parser : IRspK22Parser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';
    private const char RepeatSeparator = '~';

    private static readonly HashSet<string> ProfileTypeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MDC_HDIALY_PROFILE_TYPE",
        "MDC_HDIALY_PROFILE_VALUE",
        "MDC_HDIALY_PROFILE_TIME",
        "MDC_HDIALY_PROFILE_EXP_HALF_TIME",
        "MDC_HDIALY_PROFILE_NAME"
    };

    public RspK22ParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        string? orderId = ParseOrderId(segments);
        string? patientMrn = ParsePatientMrn(segments);
        string? modality = ParseModality(segments);
        string? orderingProvider = ParseOrderingProvider(segments);
        string? callbackPhone = ParseCallbackPhone(segments);
        string? queryTag = ParseQueryTag(segments);
        string? msaCode = ParseMsaCode(segments);
        string? msaControlId = ParseMsaControlId(segments);
        string? qpdQueryName = ParseQpdQueryName(segments);

        var settings = ParseObxSettings(segments);

        MedicalRecordNumber mrn = string.IsNullOrWhiteSpace(patientMrn)
            ? throw new ArgumentException("RSP^K22 must contain patient MRN in QPD-3 or PID-3.", nameof(hl7Message))
            : new MedicalRecordNumber(patientMrn);

        orderId ??= Ulid.NewUlid().ToString();

        return new RspK22ParseResult(
            orderId,
            mrn,
            modality,
            orderingProvider,
            callbackPhone,
            queryTag,
            msaCode,
            msaControlId,
            qpdQueryName,
            settings);
    }

    private static string? ParseOrderId(string[] segments)
    {
        string? orc = FindFirstSegment(segments, "ORC");
        if (orc is null) return null;
        string? orc2 = ExtractField(orc, 2);
        string? orc3 = ExtractField(orc, 3);
        return !string.IsNullOrEmpty(orc2) ? orc2.Split(ComponentSeparator)[0] : ExtractCodedPart(orc3 ?? string.Empty, 0);
    }

    private static string? ParsePatientMrn(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is not null)
        {
            string? qpd3 = ExtractField(qpd, 3);
            if (!string.IsNullOrEmpty(qpd3))
            {
                string[] parts = qpd3.Split(ComponentSeparator);
                if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                    return parts[1].Trim();
            }
        }

        string? pid = FindFirstSegment(segments, "PID");
        if (pid is null) return null;
        return ExtractCodedPart(ExtractField(pid, 3) ?? string.Empty, 0);
    }

    private static string? ParseModality(string[] segments) =>
        FindObxByCode(segments, "MDC_HDIALY_MACH_TX_MODALITY");

    private static string? ParseOrderingProvider(string[] segments)
    {
        string? orc = FindFirstSegment(segments, "ORC");
        if (orc is null) return null;
        return ExtractField(orc, 12);
    }

    private static string? ParseCallbackPhone(string[] segments)
    {
        string? orc = FindFirstSegment(segments, "ORC");
        if (orc is null) return null;
        return ExtractField(orc, 14);
    }

    private static string? ParseQueryTag(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        return qpd is null ? null : ExtractField(qpd, 2);
    }

    private static string? ParseMsaCode(string[] segments)
    {
        string? msa = FindFirstSegment(segments, "MSA");
        return msa is null ? null : ExtractField(msa, 1);
    }

    private static string? ParseMsaControlId(string[] segments)
    {
        string? msa = FindFirstSegment(segments, "MSA");
        return msa is null ? null : ExtractField(msa, 2);
    }

    private static string? ParseQpdQueryName(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is null) return null;
        string? qpd1 = ExtractField(qpd, 1);
        return string.IsNullOrEmpty(qpd1) ? null : ExtractCodedPart(qpd1, 0);
    }

    private static List<ProfileSetting> ParseObxSettings(string[] segments)
    {
        var settings = new List<ProfileSetting>();
        var profileBuffer = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (string seg in segments)
        {
            if (!seg.StartsWith("OBX", StringComparison.Ordinal)) continue;

            string code = ExtractCodedPart(ExtractField(seg, 3) ?? string.Empty, 1);
            if (string.IsNullOrEmpty(code)) continue;

            string? value = ExtractField(seg, 5);
            string? subId = ExtractField(seg, 4);
            string? provenance = ExtractField(seg, 17);

            if (ProfileTypeCodes.Contains(code))
            {
                profileBuffer[code] = value;
                continue;
            }

            FlushProfileBuffer(profileBuffer, subId, provenance, settings);
            profileBuffer.Clear();

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numVal)) settings.Add(ProfileSetting.Constant(code, numVal, subId, provenance));
        }

        FlushProfileBuffer(profileBuffer, null, null, settings);
        return settings;
    }

    private static void FlushProfileBuffer(Dictionary<string, string?> buffer, string? subId, string? provenance, List<ProfileSetting> settings)
    {
        if (buffer.Count == 0) return;

        string? typeStr = buffer.GetValueOrDefault("MDC_HDIALY_PROFILE_TYPE");
        if (string.IsNullOrEmpty(typeStr)) return;

        var type = new ProfileType(typeStr);
        IReadOnlyList<decimal> values = ParseDecimalArray(buffer.GetValueOrDefault("MDC_HDIALY_PROFILE_VALUE"));
        IReadOnlyList<decimal>? times = ParseNullableDecimalArray(buffer.GetValueOrDefault("MDC_HDIALY_PROFILE_TIME"));
        decimal? halfTime = ParseNullableDecimal(buffer.GetValueOrDefault("MDC_HDIALY_PROFILE_EXP_HALF_TIME"));
        string? vendorName = buffer.GetValueOrDefault("MDC_HDIALY_PROFILE_NAME");

        if (values.Count == 0 && type == ProfileType.Vendor && !string.IsNullOrEmpty(vendorName))
            values = [0];

        if (values.Count == 0) return;

        var descriptor = new ProfileDescriptor(type, values, times, halfTime, vendorName);
        settings.Add(ProfileSetting.Profiled("MDC_HDIALY_PROFILE", descriptor, subId, provenance));
    }

    private static IReadOnlyList<decimal> ParseDecimalArray(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return [];
        return raw.Split(RepeatSeparator, '^', '&')
            .Select(s => decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
    }

    private static IReadOnlyList<decimal>? ParseNullableDecimalArray(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var list = ParseDecimalArray(raw);
        return list.Count == 0 ? null : list;
    }

    private static decimal? ParseNullableDecimal(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v) ? v : null;
    }

    private static string? FindObxByCode(string[] segments, string mdcCode)
    {
        foreach (string seg in segments)
        {
            if (!seg.StartsWith("OBX", StringComparison.Ordinal)) continue;
            string code = ExtractCodedPart(ExtractField(seg, 3) ?? string.Empty, 1);
            if (string.Equals(code, mdcCode, StringComparison.OrdinalIgnoreCase))
                return ExtractField(seg, 5);
        }
        return null;
    }

    private static string ExtractCodedPart(string ce, int index)
    {
        if (string.IsNullOrEmpty(ce)) return string.Empty;
        string[] parts = ce.Split(ComponentSeparator);
        return index < parts.Length ? parts[index].Trim() : string.Empty;
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
