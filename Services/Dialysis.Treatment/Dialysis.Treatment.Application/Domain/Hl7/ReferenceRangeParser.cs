using System.Globalization;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Hl7;

/// <summary>
/// Parses OBX-7 reference range strings into structured values.
/// Formats: <c>> lower</c>, <c>&lt; upper</c>, <c>lower-upper</c>.
/// </summary>
public static class ReferenceRangeParser
{
    /// <summary>
    /// Attempts to parse OBX-7 reference range.
    /// </summary>
    /// <param name="raw">Raw OBX-7 value (e.g. "20-400", "> 20", "&lt; 400").</param>
    /// <returns>Parsed range, or null if unparseable.</returns>
    public static ReferenceRangeInfo? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        string trimmed = raw.Trim();

        if (trimmed.StartsWith('>'))
        {
            if (double.TryParse(trimmed[1..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lower))
                return new ReferenceRangeInfo(lower, null, ReferenceRangeKind.GreaterThanLower);
        }
        else if (trimmed.StartsWith('<'))
        {
            if (double.TryParse(trimmed[1..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double upper))
                return new ReferenceRangeInfo(null, upper, ReferenceRangeKind.LessThanUpper);
        }
        else
        {
            int dash = trimmed.IndexOf('-');
            if (dash >= 0)
            {
                string lowPart = trimmed[..dash].Trim();
                string highPart = trimmed[(dash + 1)..].Trim();
                if (double.TryParse(lowPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double lower) &&
                    double.TryParse(highPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double upper))
                    return new ReferenceRangeInfo(lower, upper, ReferenceRangeKind.Bounded);
            }
        }

        return null;
    }
}
