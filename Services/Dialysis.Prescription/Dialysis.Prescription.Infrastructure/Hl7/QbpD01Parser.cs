using Dialysis.Prescription.Application.Abstractions;

namespace Dialysis.Prescription.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 QBP^D01 prescription query messages.
/// Structure: MSH, QPD, RCP.
/// MSH-10 = Message Control ID; QPD-1 = Query name; QPD-2 = Query tag; QPD-3 = @PID.3^{MRN}^^^^MR
/// </summary>
public sealed class QbpD01Parser : IQbpD01Parser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    public QbpD01ParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        string? messageControlId = ParseMessageControlId(segments);
        string? queryTag = ParseQueryTag(segments);
        string? queryName = ParseQueryName(segments);
        string mrn = ParseMrn(segments);

        if (string.IsNullOrWhiteSpace(mrn))
            throw new ArgumentException("QBP^D01 must contain patient MRN in QPD-3.", nameof(hl7Message));

        return new QbpD01ParseResult(mrn.Trim(), messageControlId, queryTag, queryName);
    }

    private static string? ParseMessageControlId(string[] segments)
    {
        string? msh = FindFirstSegment(segments, "MSH");
        if (msh is null) return null;
        string[] f = msh.Split(FieldSeparator);
        return f.Length > 9 ? f[9].Trim() : null;
    }

    private static string? ParseQueryTag(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        return qpd is null ? null : ExtractFieldHl7(qpd, 2);
    }

    private static string? ParseQueryName(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is null) return null;
        string? qpd1 = ExtractFieldHl7(qpd, 1);
        return string.IsNullOrEmpty(qpd1) ? null : qpd1.Split(ComponentSeparator)[0].Trim();
    }

    private static string ParseMrn(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is null) return string.Empty;

        string? qpd3 = ExtractFieldHl7(qpd, 3);
        string? qpd4 = ExtractFieldHl7(qpd, 4);

        if (!string.IsNullOrEmpty(qpd3))
        {
            string[] parts = qpd3.Split(ComponentSeparator);
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                return parts[1].Trim();
        }

        if (!string.IsNullOrEmpty(qpd4))
        {
            string[] parts = qpd4.Split(ComponentSeparator);
            return parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0].Trim() : string.Empty;
        }

        return string.Empty;
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

    /// <summary>
    /// Extract HL7 field by 1-based index. Segment ID is [0]; MSH-1 = [1], QPD-1 = [1], etc.
    /// </summary>
    private static string? ExtractFieldHl7(string segment, int hl7FieldIndex)
    {
        if (hl7FieldIndex < 1) return null;
        string[] fields = segment.Split(FieldSeparator);
        if (hl7FieldIndex >= fields.Length) return null;
        return fields[hl7FieldIndex].Trim();
    }
}
