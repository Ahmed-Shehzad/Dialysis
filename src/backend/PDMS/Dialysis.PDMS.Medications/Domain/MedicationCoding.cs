namespace Dialysis.PDMS.Medications.Domain;

/// <summary>
/// Coded medication identity. Mirrors the value-object shape HIS / EHR use so cross-module
/// projection is mechanical. Codes can come from RxNorm, NDC, or the German ATC system.
/// Modelled as a reference record (not a struct) so EF Core can treat it as an owned type
/// — both <see cref="MedicationAdministrationEntry"/> and <see cref="MedicationInventoryItem"/>
/// persist their coding inline alongside the parent row.
/// </summary>
public sealed record MedicationCoding(string CodeSystem, string Code, string DisplayName)
{
    public static MedicationCoding RxNorm(string code, string displayName) =>
        new("http://www.nlm.nih.gov/research/umls/rxnorm", code, displayName);

    public static MedicationCoding Atc(string code, string displayName) =>
        new("http://www.whocc.no/atc", code, displayName);

    public static MedicationCoding Ndc(string code, string displayName) =>
        new("http://hl7.org/fhir/sid/ndc", code, displayName);
}

/// <summary>Dose value object — quantity + unit, no derivation logic here.</summary>
public sealed record Dose(decimal Quantity, string Unit)
{
    public static Dose Milligrams(decimal mg) => new(mg, "mg");
    public static Dose Milliliters(decimal ml) => new(ml, "mL");
    public static Dose Units(decimal units) => new(units, "U");
}

/// <summary>Routes of administration supported at the chairside.</summary>
public enum MedicationRoute
{
    Intravenous = 0,
    IntravenousPump = 1,
    Subcutaneous = 2,
    Oral = 3,
    Topical = 4,
    Other = 99,
}
