using System.Globalization;

using Dialysis.Patient.Application.Abstractions;

namespace Dialysis.Patient.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 RSP^K22 patient demographics response messages (IHE ITI-21).
/// Structure: MSH, MSA (R), QAK (R), QPD (R), PID (0..N).
/// </summary>
public sealed class RspK22PatientParser : IRspK22PatientParser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    public RspK22PatientParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        string msaAckCode = ExtractMsaAckCode(segments);
        string msaControlId = ExtractMsaControlId(segments);
        string qakQueryTag = ExtractQakQueryTag(segments);
        string qakStatus = ExtractQakStatus(segments);
        string? qakQueryName = ExtractQakQueryName(segments);
        int qakHitCount = ExtractQakHitCount(segments);

        List<PidPatientData> patients = ParsePidSegments(segments);

        return new RspK22PatientParseResult(
            msaAckCode,
            msaControlId,
            qakQueryTag,
            qakStatus,
            qakQueryName,
            qakHitCount,
            patients);
    }

    private static string ExtractMsaAckCode(string[] segments)
    {
        string? msa = FindFirstSegment(segments, "MSA");
        if (msa is null) return "AE";
        string? code = ExtractField(msa, 1);
        return string.IsNullOrWhiteSpace(code) ? "AE" : code.Trim();
    }

    private static string ExtractMsaControlId(string[] segments)
    {
        string? msa = FindFirstSegment(segments, "MSA");
        string? id = msa is null ? null : ExtractField(msa, 2);
        return id?.Trim() ?? string.Empty;
    }

    private static string ExtractQakQueryTag(string[] segments)
    {
        string? qak = FindFirstSegment(segments, "QAK");
        string? tag = qak is null ? null : ExtractField(qak, 1);
        return tag?.Trim() ?? string.Empty;
    }

    private static string ExtractQakStatus(string[] segments)
    {
        string? qak = FindFirstSegment(segments, "QAK");
        string? status = qak is null ? null : ExtractField(qak, 2);
        return string.IsNullOrWhiteSpace(status) ? "AE" : status.Trim();
    }

    private static string? ExtractQakQueryName(string[] segments)
    {
        string? qak = FindFirstSegment(segments, "QAK");
        string? name = qak is null ? null : ExtractField(qak, 3);
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private static int ExtractQakHitCount(string[] segments)
    {
        string? qak = FindFirstSegment(segments, "QAK");
        string? countStr = qak is null ? null : ExtractField(qak, 4);
        return int.TryParse(countStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) ? count : 0;
    }

    private static List<PidPatientData> ParsePidSegments(string[] segments)
    {
        var patients = new List<PidPatientData>();
        foreach (string seg in segments)
        {
            if (!seg.StartsWith("PID" + FieldSeparator, StringComparison.Ordinal) && !seg.Equals("PID", StringComparison.Ordinal))
                continue;

            PidPatientData? data = ParsePidSegment(seg);
            if (data is not null)
                patients.Add(data);
        }
        return patients;
    }

    private static PidPatientData? ParsePidSegment(string pidSegment)
    {
        string? pid3 = ExtractField(pidSegment, 3);
        if (string.IsNullOrWhiteSpace(pid3))
            return null;

        string? identifier = null;
        string? identifierType = null;
        string[] pid3Parts = pid3.Split(ComponentSeparator);
        if (pid3Parts.Length >= 1 && !string.IsNullOrWhiteSpace(pid3Parts[0]))
            identifier = pid3Parts[0].Trim();
        if (pid3Parts.Length >= 5 && !string.IsNullOrWhiteSpace(pid3Parts[4]))
            identifierType = pid3Parts[4].Trim();

        string? pid5 = ExtractField(pidSegment, 5);
        string? lastName = null;
        string? firstName = null;
        if (!string.IsNullOrWhiteSpace(pid5))
        {
            string[] nameParts = pid5.Split(ComponentSeparator);
            if (nameParts.Length >= 1) lastName = NullIfEmpty(nameParts[0]);
            if (nameParts.Length >= 2) firstName = NullIfEmpty(nameParts[1]);
        }

        string? pid7 = ExtractField(pidSegment, 7);
        string? dob = NullIfEmpty(pid7);

        string? pid8 = ExtractField(pidSegment, 8);
        string? gender = NullIfEmpty(pid8);

        return new PidPatientData(identifier, identifierType, lastName, firstName, dob, gender);
    }

    private static string[] SplitSegments(string hl7Message)
    {
        string normalized = hl7Message.Replace("\r\n", "\r").Replace("\n", "\r");
        return normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string? FindFirstSegment(string[] segments, string segmentType)
    {
        foreach (string seg in segments)
            if (seg.StartsWith(segmentType + FieldSeparator, StringComparison.Ordinal) ||
                seg.Equals(segmentType, StringComparison.Ordinal))
                return seg;

        return null;
    }

    private static string? ExtractField(string segment, int hl7FieldIndex)
    {
        if (hl7FieldIndex < 1) return null;
        string[] fields = segment.Split(FieldSeparator);
        return hl7FieldIndex < fields.Length ? fields[hl7FieldIndex].Trim() : null;
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
