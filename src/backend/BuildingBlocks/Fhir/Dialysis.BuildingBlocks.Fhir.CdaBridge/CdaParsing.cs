using System.Globalization;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Low-level CDA datatype parsing shared across the section parsers: coded concepts (CD/CE),
/// timestamps (TS) and intervals (IVL_TS), and nullFlavor handling. Each helper is null-soft —
/// a missing or <c>nullFlavor</c>-marked element yields <c>null</c> rather than throwing, so a
/// partially-populated partner document still maps the sections it does carry.
/// </summary>
internal static class CdaParsing
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;
    private static readonly XNamespace _xsi = CdaConstants.Xsi;

    /// <summary>True when the element is absent or carries a <c>nullFlavor</c> (NI/UNK/NA/…).</summary>
    public static bool IsNull(XElement? element) =>
        element is null || element.Attribute("nullFlavor") is not null;

    /// <summary>
    /// Parses a CDA coded element (<c>code</c> / <c>value xsi:type="CD"</c>) into a FHIR
    /// <see cref="CodeableConcept"/>. Honours <c>originalText</c> and any <c>translation</c>
    /// children as additional codings. Returns <c>null</c> for a missing / nullFlavor element.
    /// </summary>
    public static CodeableConcept? ParseCodeableConcept(XElement? coded)
    {
        if (IsNull(coded)) return null;

        var concept = new CodeableConcept();
        var primary = ParseCoding(coded!);
        if (primary is not null) concept.Coding.Add(primary);

        foreach (var translation in coded!.Elements(_hl7 + "translation"))
        {
            var translationCoding = ParseCoding(translation);
            if (translationCoding is not null) concept.Coding.Add(translationCoding);
        }

        var originalText = coded.Element(_hl7 + "originalText")?.Value?.Trim();
        if (!string.IsNullOrEmpty(originalText)) concept.Text = originalText;
        else if (!string.IsNullOrEmpty(primary?.Display)) concept.Text = primary.Display;

        return concept.Coding.Count == 0 && string.IsNullOrEmpty(concept.Text) ? null : concept;
    }

    private static Coding? ParseCoding(XElement coded)
    {
        var code = coded.Attribute("code")?.Value;
        var system = coded.Attribute("codeSystem")?.Value;
        var display = coded.Attribute("displayName")?.Value;
        if (string.IsNullOrWhiteSpace(code)) return null;
        return new Coding(CdaConstants.OidToUri(system), code, display);
    }

    /// <summary>Reads the <c>xsi:type</c> discriminator of a CDA <c>value</c> element.</summary>
    public static string? ValueType(XElement value) =>
        value.Attribute(_xsi + "type")?.Value;

    /// <summary>
    /// Parses a physical-quantity value (<c>value xsi:type="PQ" value="120" unit="mmHg"</c>)
    /// into a FHIR <see cref="Quantity"/>. Returns <c>null</c> when not a parseable PQ.
    /// </summary>
    public static Quantity? ParseQuantity(XElement? value)
    {
        if (IsNull(value)) return null;
        var raw = value!.Attribute("value")?.Value;
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            return null;
        var unit = value.Attribute("unit")?.Value;
        return new Quantity { Value = amount, Unit = unit, Code = unit, System = "http://unitsofmeasure.org" };
    }

    /// <summary>
    /// Converts a CDA <c>TS</c> string (<c>yyyy</c>, <c>yyyyMM</c>, <c>yyyyMMdd</c>,
    /// <c>yyyyMMddHHmmss[±zzzz]</c>) into a FHIR date/time literal. Returns <c>null</c> when the
    /// value is empty or too short to carry a year.
    /// </summary>
    public static string? ParseTimestamp(string? ts)
    {
        if (string.IsNullOrWhiteSpace(ts) || ts.Length < 4) return null;
        var span = ts.AsSpan();
        var year = span[..4].ToString();
        if (span.Length < 6) return year;
        var month = span.Slice(4, 2).ToString();
        if (span.Length < 8) return $"{year}-{month}";
        var day = span.Slice(6, 2).ToString();
        if (span.Length < 14) return $"{year}-{month}-{day}";
        var hour = span.Slice(8, 2).ToString();
        var minute = span.Slice(10, 2).ToString();
        var second = span.Slice(12, 2).ToString();
        var zone = ParseZone(ts);
        return $"{year}-{month}-{day}T{hour}:{minute}:{second}{zone}";
    }

    private static string ParseZone(string ts)
    {
        var plus = ts.IndexOfAny(['+', '-'], 14 <= ts.Length ? 14 : ts.Length - 1);
        if (plus < 0 || plus + 5 > ts.Length) return "+00:00";
        var sign = ts[plus];
        var hh = ts.Substring(plus + 1, 2);
        var mm = ts.Substring(plus + 3, 2);
        return $"{sign}{hh}:{mm}";
    }

    /// <summary>Reads the effective instant of an entry — point <c>value</c> or interval <c>low</c>.</summary>
    public static string? ParseEffectiveInstant(XElement? effectiveTime)
    {
        if (IsNull(effectiveTime)) return null;
        var pointValue = effectiveTime!.Attribute("value")?.Value;
        if (!string.IsNullOrWhiteSpace(pointValue)) return ParseTimestamp(pointValue);
        var low = effectiveTime.Element(_hl7 + "low")?.Attribute("value")?.Value;
        return ParseTimestamp(low);
    }

    /// <summary>Reads an effective interval (<c>low</c>/<c>high</c>) as a FHIR <see cref="Period"/>.</summary>
    public static Period? ParseEffectivePeriod(XElement? effectiveTime)
    {
        if (IsNull(effectiveTime)) return null;
        var low = ParseTimestamp(effectiveTime!.Element(_hl7 + "low")?.Attribute("value")?.Value);
        var high = ParseTimestamp(effectiveTime.Element(_hl7 + "high")?.Attribute("value")?.Value);
        if (low is null && high is null) return null;
        return new Period { StartElement = low is null ? null : new FhirDateTime(low), EndElement = high is null ? null : new FhirDateTime(high) };
    }
}
