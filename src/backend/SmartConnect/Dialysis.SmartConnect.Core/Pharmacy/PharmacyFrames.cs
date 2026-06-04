namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// A coded pharmacy product (CWE triplet: identifier ^ text ^ coding-system). Carries the
/// RxNorm / ATC / local code an external pharmacy system needs to reconcile the
/// administration or give message against its own formulary.
/// </summary>
public sealed record PharmacyMedication
{
    /// <summary>
    /// A coded pharmacy product (CWE triplet: identifier ^ text ^ coding-system). Carries the
    /// RxNorm / ATC / local code an external pharmacy system needs to reconcile the
    /// administration or give message against its own formulary.
    /// </summary>
    public PharmacyMedication(string Code, string Display, string System)
    {
        this.Code = Code;
        this.Display = Display;
        this.System = System;
    }
    public string Code { get; init; }
    public string Display { get; init; }
    public string System { get; init; }
    public void Deconstruct(out string Code, out string Display, out string System)
    {
        Code = this.Code;
        Display = this.Display;
        System = this.System;
    }
}

/// <summary>
/// IO-free input for <see cref="Hl7V2RasO17Builder"/> — one positive medication
/// administration recorded at the chairside. Mirrors the wire-relevant fields of the
/// upstream <c>MedicationAdministeredIntegrationEvent</c> without coupling SmartConnect to
/// the PDMS module (the transform stage deserialises the event JSON into this frame).
/// </summary>
public sealed record MedicationAdministrationFrame
{
    /// <summary>
    /// IO-free input for <see cref="Hl7V2RasO17Builder"/> — one positive medication
    /// administration recorded at the chairside. Mirrors the wire-relevant fields of the
    /// upstream <c>MedicationAdministeredIntegrationEvent</c> without coupling SmartConnect to
    /// the PDMS module (the transform stage deserialises the event JSON into this frame).
    /// </summary>
    public MedicationAdministrationFrame(string PatientIdentifier,
        string? PlacerOrderNumber,
        PharmacyMedication Medication,
        decimal DoseQuantity,
        string DoseUnit,
        string Route,
        DateTime AdministeredAtUtc,
        string AdministeredBy,
        string SendingApplication = "DIALYSIS_PDMS")
    {
        this.PatientIdentifier = PatientIdentifier;
        this.PlacerOrderNumber = PlacerOrderNumber;
        this.Medication = Medication;
        this.DoseQuantity = DoseQuantity;
        this.DoseUnit = DoseUnit;
        this.Route = Route;
        this.AdministeredAtUtc = AdministeredAtUtc;
        this.AdministeredBy = AdministeredBy;
        this.SendingApplication = SendingApplication;
    }
    public string PatientIdentifier { get; init; }
    public string? PlacerOrderNumber { get; init; }
    public PharmacyMedication Medication { get; init; }
    public decimal DoseQuantity { get; init; }
    public string DoseUnit { get; init; }
    public string Route { get; init; }
    public DateTime AdministeredAtUtc { get; init; }
    public string AdministeredBy { get; init; }
    public string SendingApplication { get; init; }
    public void Deconstruct(out string PatientIdentifier, out string? PlacerOrderNumber, out PharmacyMedication Medication, out decimal DoseQuantity, out string DoseUnit, out string Route, out DateTime AdministeredAtUtc, out string AdministeredBy, out string SendingApplication)
    {
        PatientIdentifier = this.PatientIdentifier;
        PlacerOrderNumber = this.PlacerOrderNumber;
        Medication = this.Medication;
        DoseQuantity = this.DoseQuantity;
        DoseUnit = this.DoseUnit;
        Route = this.Route;
        AdministeredAtUtc = this.AdministeredAtUtc;
        AdministeredBy = this.AdministeredBy;
        SendingApplication = this.SendingApplication;
    }
}

/// <summary>
/// IO-free input for <see cref="Hl7V2RgvO15Builder"/> — one give/refusal record. We reuse
/// the pharmacy-give trigger to carry a clinical decline: the give segment names the
/// ordered product and <see cref="Reason"/> rides an NTE refusal note so the receiving
/// pharmacy system records that the dose was prepared-but-not-given.
/// </summary>
public sealed record MedicationGiveFrame
{
    /// <summary>
    /// IO-free input for <see cref="Hl7V2RgvO15Builder"/> — one give/refusal record. We reuse
    /// the pharmacy-give trigger to carry a clinical decline: the give segment names the
    /// ordered product and <see cref="Reason"/> rides an NTE refusal note so the receiving
    /// pharmacy system records that the dose was prepared-but-not-given.
    /// </summary>
    public MedicationGiveFrame(string PatientIdentifier,
        string? PlacerOrderNumber,
        PharmacyMedication Medication,
        DateTime GiveAtUtc,
        string RecordedBy,
        string Reason,
        string SendingApplication = "DIALYSIS_PDMS")
    {
        this.PatientIdentifier = PatientIdentifier;
        this.PlacerOrderNumber = PlacerOrderNumber;
        this.Medication = Medication;
        this.GiveAtUtc = GiveAtUtc;
        this.RecordedBy = RecordedBy;
        this.Reason = Reason;
        this.SendingApplication = SendingApplication;
    }
    public string PatientIdentifier { get; init; }
    public string? PlacerOrderNumber { get; init; }
    public PharmacyMedication Medication { get; init; }
    public DateTime GiveAtUtc { get; init; }
    public string RecordedBy { get; init; }
    public string Reason { get; init; }
    public string SendingApplication { get; init; }
    public void Deconstruct(out string PatientIdentifier, out string? PlacerOrderNumber, out PharmacyMedication Medication, out DateTime GiveAtUtc, out string RecordedBy, out string Reason, out string SendingApplication)
    {
        PatientIdentifier = this.PatientIdentifier;
        PlacerOrderNumber = this.PlacerOrderNumber;
        Medication = this.Medication;
        GiveAtUtc = this.GiveAtUtc;
        RecordedBy = this.RecordedBy;
        Reason = this.Reason;
        SendingApplication = this.SendingApplication;
    }
}
