using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Low-level CDA emit helpers — the inverse of <see cref="CdaParsing"/>. Builds <c>code</c> /
/// typed <c>value</c> elements and CDA <c>TS</c> timestamps from FHIR datatypes, so the section
/// emitters can stay declarative.
/// </summary>
internal static class CdaEmitting
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;
    private static readonly XNamespace _xsi = CdaConstants.Xsi;

    /// <summary>Builds a CDA coded element (<c>code</c>/<c>value</c>) from a FHIR concept's first coding.</summary>
    public static XElement CodeElement(string elementName, CodeableConcept? concept)
    {
        var element = new XElement(_hl7 + elementName);
        var coding = concept?.Coding?.FirstOrDefault();
        if (coding is not null)
        {
            if (!string.IsNullOrEmpty(coding.Code)) element.SetAttributeValue("code", coding.Code);
            var oid = CdaConstants.UriToOid(coding.System);
            if (!string.IsNullOrEmpty(oid)) element.SetAttributeValue("codeSystem", oid);
            if (!string.IsNullOrEmpty(coding.Display)) element.SetAttributeValue("displayName", coding.Display);
        }
        else if (!string.IsNullOrEmpty(concept?.Text))
        {
            element.Add(new XElement(_hl7 + "originalText", concept.Text));
        }
        return element;
    }

    /// <summary>Builds a typed <c>value</c> element from an Observation value (PQ / CD / ST).</summary>
    public static XElement ValueElement(DataType? value)
    {
        var element = new XElement(_hl7 + "value");
        switch (value)
        {
            case Quantity quantity:
                element.SetAttributeValue(_xsi + "type", "PQ");
                if (quantity.Value.HasValue) element.SetAttributeValue("value", quantity.Value.Value);
                if (!string.IsNullOrEmpty(quantity.Unit)) element.SetAttributeValue("unit", quantity.Unit);
                break;
            case CodeableConcept concept:
                element.SetAttributeValue(_xsi + "type", "CD");
                var coding = concept.Coding?.FirstOrDefault();
                if (coding is not null)
                {
                    if (!string.IsNullOrEmpty(coding.Code)) element.SetAttributeValue("code", coding.Code);
                    var oid = CdaConstants.UriToOid(coding.System);
                    if (!string.IsNullOrEmpty(oid)) element.SetAttributeValue("codeSystem", oid);
                    if (!string.IsNullOrEmpty(coding.Display)) element.SetAttributeValue("displayName", coding.Display);
                }
                break;
            case FhirString text:
                element.SetAttributeValue(_xsi + "type", "ST");
                element.Value = text.Value ?? string.Empty;
                break;
            default:
                element.SetAttributeValue("nullFlavor", "UNK");
                break;
        }
        return element;
    }

    /// <summary>Emits an <c>effectiveTime</c> with a point <c>value</c> from a FHIR date/time.</summary>
    public static XElement EffectiveTimePoint(string elementName, DataType? when)
    {
        var element = new XElement(_hl7 + elementName);
        var ts = ToCdaTimestamp(when);
        if (ts is not null) element.SetAttributeValue("value", ts);
        else element.SetAttributeValue("nullFlavor", "UNK");
        return element;
    }

    /// <summary>Emits an <c>effectiveTime</c> with <c>low</c>/<c>high</c> from a FHIR Period.</summary>
    public static XElement EffectiveTimeInterval(Period? period)
    {
        var element = new XElement(_hl7 + "effectiveTime");
        var low = ToCdaTimestamp(period?.StartElement);
        var high = ToCdaTimestamp(period?.EndElement);
        if (low is not null) element.Add(new XElement(_hl7 + "low", new XAttribute("value", low)));
        if (high is not null) element.Add(new XElement(_hl7 + "high", new XAttribute("value", high)));
        if (low is null && high is null) element.SetAttributeValue("nullFlavor", "UNK");
        return element;
    }

    /// <summary>Strips a FHIR date/dateTime to the CDA <c>TS</c> digit form (<c>yyyyMMdd[HHmmss]</c>).</summary>
    public static string? ToCdaTimestamp(DataType? when)
    {
        var raw = when switch
        {
            FhirDateTime dt => dt.Value,
            Date d => d.Value,
            FhirString s => s.Value,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Keep only the date+time digits, dropping separators and any timezone suffix.
        var digits = new string(raw.TakeWhile(c => c is not ('+' or 'Z'))
            .Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits : null;
    }
}
