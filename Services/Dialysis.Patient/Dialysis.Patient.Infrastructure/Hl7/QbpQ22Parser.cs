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

        QpdDemographics demographics = ParseQpdDemographics(segments);
        (string? mrn, string? personNumber, string? socialSecurityNumber, string? universalId, string? firstName, string? lastName, DateOnly? birthdate) = demographics;

        if (string.IsNullOrWhiteSpace(mrn) && string.IsNullOrWhiteSpace(personNumber) && string.IsNullOrWhiteSpace(socialSecurityNumber) && string.IsNullOrWhiteSpace(universalId) && string.IsNullOrWhiteSpace(lastName) && !birthdate.HasValue)
            throw new ArgumentException("QBP^Q22 must contain patient MRN (@PID.3), name (@PID.5), or birthdate (@PID.7) in QPD-3.", nameof(hl7Message));

        return new QbpQ22ParseResult(mrn?.Trim(), firstName?.Trim(), lastName?.Trim(), messageControlId, queryTag, queryName, personNumber?.Trim(), socialSecurityNumber?.Trim(), universalId?.Trim(), birthdate);
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

    private static QpdDemographics ParseQpdDemographics(string[] segments)
    {
        var d = new QpdDemographics();

        string? qpd = FindFirstSegment(segments, "QPD");
        if (qpd is null) return d;

        string? qpd3 = ExtractField(qpd, 3);
        if (string.IsNullOrEmpty(qpd3)) return d;

        string[] queryParams = qpd3.Split(RepeatSeparator);
        foreach (string param in queryParams)
            ApplyQueryParam(param, ref d);

        TryApplyFallbackMrn(qpd3, queryParams, ref d);
        return d;
    }

    private static void ApplyQueryParam(string param, ref QpdDemographics d)
    {
        string[] parts = param.Split(ComponentSeparator);
        if (parts.Length < 2) return;

        string fieldRef = parts[0].Trim();
        string value = parts[1].Trim();
        string? identifierType = parts.Length >= 6 ? NullIfEmpty(parts[5]) : null;

        if (IsPid3Field(fieldRef))
            ApplyPid3Identifier(identifierType, value, ref d);
        else if (fieldRef.Equals("@PID.5.1", StringComparison.OrdinalIgnoreCase))
            d.LastName = value;
        else if (fieldRef.Equals("@PID.5.2", StringComparison.OrdinalIgnoreCase))
            d.FirstName = value;
        else if (fieldRef.Equals("@PID.7", StringComparison.OrdinalIgnoreCase) && value.Length >= 8)
            d.Birthdate = ParseBirthdate(value);
    }

    private static DateOnly? ParseBirthdate(string value)
    {
        if (value.Length < 8) return null;
        if (int.TryParse(value.AsSpan(0, 4), out int y) && int.TryParse(value.AsSpan(4, 2), out int m) && int.TryParse(value.AsSpan(6, 2), out int day))
            try { return new DateOnly(y, m, day); }
            catch { return null; }

        return null;
    }

    private static bool IsPid3Field(string fieldRef) =>
        fieldRef.Equals("@PID.3.1", StringComparison.OrdinalIgnoreCase) || fieldRef.Equals("@PID.3", StringComparison.OrdinalIgnoreCase);

    private static void ApplyPid3Identifier(string? identifierType, string value, ref QpdDemographics d)
    {
        switch (identifierType?.ToUpperInvariant())
        {
            case "PN": d.PersonNumber = value; break;
            case "SS": d.SocialSecurityNumber = value; break;
            case "U": d.UniversalId = value; break;
            default: d.Mrn = value; break;
        }
    }

    private static void TryApplyFallbackMrn(string qpd3, string[] queryParams, ref QpdDemographics d)
    {
        if (d.HasAnyValue || queryParams.Length == 0) return;

        string[] fallback = qpd3.Split(ComponentSeparator);
        if (fallback.Length >= 2 && !string.IsNullOrWhiteSpace(fallback[1]))
            d.Mrn = fallback[1].Trim();
    }

    private sealed class QpdDemographics
    {
        internal string? Mrn;
        internal string? PersonNumber;
        internal string? SocialSecurityNumber;
        internal string? UniversalId;
        internal string? FirstName;
        internal string? LastName;
        internal DateOnly? Birthdate;

        internal bool HasAnyValue => Mrn is not null || PersonNumber is not null || SocialSecurityNumber is not null || UniversalId is not null || FirstName is not null || LastName is not null || Birthdate is not null;

        internal void Deconstruct(out string? mrn, out string? personNumber, out string? socialSecurityNumber, out string? universalId, out string? firstName, out string? lastName, out DateOnly? birthdate)
        {
            mrn = Mrn;
            personNumber = PersonNumber;
            socialSecurityNumber = SocialSecurityNumber;
            universalId = UniversalId;
            firstName = FirstName;
            lastName = LastName;
            birthdate = Birthdate;
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
