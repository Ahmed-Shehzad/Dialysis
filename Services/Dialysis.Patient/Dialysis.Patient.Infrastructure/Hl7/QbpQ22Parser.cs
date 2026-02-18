using Dialysis.Patient.Application.Abstractions;

namespace Dialysis.Patient.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 QBP^Q22 patient demographics query messages (IHE ITI-21).
/// QPD-3: @PID.3.1^{MRN} or @PID.5.1^{LastName} &amp; @PID.5.2^{FirstName}.
/// </summary>
public sealed class QbpQ22Parser : IQbpQ22Parser
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';
    private const char RepeatSeparator = '~';

    public QbpQ22ParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        string[] segments = SplitSegments(hl7Message);

        string? messageControlId = ParseMessageControlId(segments);
        string? queryTag = ParseQueryTag(segments);
        string? queryName = ParseQueryName(segments);

        ParseQpdDemographics(segments, out string? mrn, out string? personNumber, out string? socialSecurityNumber, out string? universalId, out string? firstName, out string? lastName);

        if (string.IsNullOrWhiteSpace(mrn) && string.IsNullOrWhiteSpace(personNumber) && string.IsNullOrWhiteSpace(socialSecurityNumber) && string.IsNullOrWhiteSpace(universalId) && string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("QBP^Q22 must contain patient MRN in QPD-3 (@PID.3) or patient name (@PID.5).", nameof(hl7Message));

        return new QbpQ22ParseResult(mrn?.Trim(), firstName?.Trim(), lastName?.Trim(), messageControlId, queryTag, queryName, personNumber?.Trim(), socialSecurityNumber?.Trim(), universalId?.Trim());
    }

    private static string? ParseMessageControlId(string[] segments)
    {
        string? msh = FindFirstSegment(segments, "MSH");
        if (msh is null) return null;
        string[] f = msh.Split(FieldSeparator);
        return f.Length > 9 ? NullIfEmpty(f[9]) : null;
    }

    private static string? ParseQueryTag(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        return qpd is null ? null : ExtractField(qpd, 2);
    }

    private static string? ParseQueryName(string[] segments)
    {
        string? qpd = FindFirstSegment(segments, "QPD");
        return qpd is null ? null : ExtractField(qpd, 1);
    }

    private static void ParseQpdDemographics(string[] segments, out string? mrn, out string? personNumber, out string? socialSecurityNumber, out string? universalId, out string? firstName, out string? lastName)
    {
        mrn = null;
        personNumber = null;
        socialSecurityNumber = null;
        universalId = null;
        firstName = null;
        lastName = null;

        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is null) return;

        string? qpd3 = ExtractField(qpd, 3);
        if (string.IsNullOrEmpty(qpd3)) return;

        string[] queryParams = qpd3.Split(RepeatSeparator);
        foreach (string param in queryParams)
        {
            string[] parts = param.Split(ComponentSeparator);
            if (parts.Length < 2) continue;

            string fieldRef = parts[0].Trim();
            string value = parts[1].Trim();
            string? identifierType = parts.Length >= 6 ? NullIfEmpty(parts[5]) : null;

            if (fieldRef.Equals("@PID.3.1", StringComparison.OrdinalIgnoreCase) || fieldRef.Equals("@PID.3", StringComparison.OrdinalIgnoreCase))
                switch (identifierType?.ToUpperInvariant())
                {
                    case "PN": personNumber = value; break;
                    case "SS": socialSecurityNumber = value; break;
                    case "U": universalId = value; break;
                    default: mrn = value; break;
                }
            else if (fieldRef.Equals("@PID.5.1", StringComparison.OrdinalIgnoreCase)) lastName = value;
            else if (fieldRef.Equals("@PID.5.2", StringComparison.OrdinalIgnoreCase)) firstName = value;
        }

        if (mrn is null && personNumber is null && socialSecurityNumber is null && universalId is null && firstName is null && lastName is null && queryParams.Length > 0)
        {
            string[] fallback = qpd3.Split(ComponentSeparator);
            if (fallback.Length >= 2 && !string.IsNullOrWhiteSpace(fallback[1]))
                mrn = fallback[1].Trim();
        }
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
        return hl7FieldIndex < fields.Length ? NullIfEmpty(fields[hl7FieldIndex]) : null;
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
