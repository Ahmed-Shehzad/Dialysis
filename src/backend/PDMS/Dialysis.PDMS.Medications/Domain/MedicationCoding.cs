namespace Dialysis.PDMS.Medications.Domain;

/// <summary>
/// Coded medication identity. Mirrors the value-object shape HIS / EHR use so cross-module
/// projection is mechanical. Codes can come from RxNorm, NDC, or the German ATC system.
/// Modelled as a reference record (not a struct) so EF Core can treat it as an owned type
/// — both <see cref="MedicationAdministrationEntry"/> and <see cref="MedicationInventoryItem"/>
/// persist their coding inline alongside the parent row.
/// </summary>
public sealed record MedicationCoding
{
    /// <summary>
    /// Coded medication identity. Mirrors the value-object shape HIS / EHR use so cross-module
    /// projection is mechanical. Codes can come from RxNorm, NDC, or the German ATC system.
    /// Modelled as a reference record (not a struct) so EF Core can treat it as an owned type
    /// — both <see cref="MedicationAdministrationEntry"/> and <see cref="MedicationInventoryItem"/>
    /// persist their coding inline alongside the parent row.
    /// </summary>
    public MedicationCoding(string CodeSystem, string Code, string DisplayName)
    {
        this.CodeSystem = CodeSystem;
        this.Code = Code;
        this.DisplayName = DisplayName;
    }
    public static MedicationCoding RxNorm(string code, string displayName) =>
        new("http://www.nlm.nih.gov/research/umls/rxnorm", code, displayName);

    public static MedicationCoding Atc(string code, string displayName) =>
        new("http://www.whocc.no/atc", code, displayName);

    public static MedicationCoding Ndc(string code, string displayName) =>
        new("http://hl7.org/fhir/sid/ndc", code, displayName);
    public string CodeSystem { get; init; }
    public string Code { get; init; }
    public string DisplayName { get; init; }
    public void Deconstruct(out string CodeSystem, out string Code, out string DisplayName)
    {
        CodeSystem = this.CodeSystem;
        Code = this.Code;
        DisplayName = this.DisplayName;
    }
}

/// <summary>Dose value object — quantity + unit, no derivation logic here.</summary>
public sealed record Dose
{
    /// <summary>Dose value object — quantity + unit, no derivation logic here.</summary>
    public Dose(decimal Quantity, string Unit)
    {
        this.Quantity = Quantity;
        this.Unit = Unit;
    }
    public static Dose Milligrams(decimal mg) => new(mg, "mg");
    public static Dose Milliliters(decimal ml) => new(ml, "mL");
    public static Dose Units(decimal units) => new(units, "U");
    public decimal Quantity { get; init; }
    public string Unit { get; init; }
    public void Deconstruct(out decimal Quantity, out string Unit)
    {
        Quantity = this.Quantity;
        Unit = this.Unit;
    }
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
