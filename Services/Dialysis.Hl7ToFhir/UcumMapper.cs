namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps HL7/OBX unit strings to FHIR UCUM-compliant codes.
/// UCUM system: http://unitsofmeasure.org.
/// </summary>
public static class UcumMapper
{
    private static readonly IReadOnlyDictionary<string, string> UnitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["mmHg"] = "mm[Hg]",
        ["mm Hg"] = "mm[Hg]",
        ["mL/min"] = "mL/min",
        ["ml/min"] = "mL/min",
        ["mL/h"] = "mL/h",
        ["ml/h"] = "mL/h",
        ["mL"] = "mL",
        ["ml"] = "mL",
        ["L"] = "L",
        ["min"] = "min",
        ["°C"] = "Cel",
        ["Cel"] = "Cel",
        ["mS/cm"] = "mS/cm",
        ["mmol/L"] = "mmol/L",
        ["kg"] = "kg",
        ["bpm"] = "/min",
        ["%"] = "%",
        ["rpm"] = "{rpm}",
        ["hours"] = "h"
    };

    /// <summary>
    /// Resolve OBX-6 unit to UCUM code for FHIR Quantity.
    /// </summary>
    public static string ToUcumCode(string? unit) => string.IsNullOrEmpty(unit) ? string.Empty : UnitMap.GetValueOrDefault(unit, unit);
}
