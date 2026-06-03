namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// A coded pharmacy product (CWE triplet: identifier ^ text ^ coding-system). Carries the
/// RxNorm / ATC / local code an external pharmacy system needs to reconcile the
/// administration or give message against its own formulary.
/// </summary>
public sealed record PharmacyMedication(string Code, string Display, string System);

/// <summary>
/// IO-free input for <see cref="Hl7V2RasO17Builder"/> — one positive medication
/// administration recorded at the chairside. Mirrors the wire-relevant fields of the
/// upstream <c>MedicationAdministeredIntegrationEvent</c> without coupling SmartConnect to
/// the PDMS module (the transform stage deserialises the event JSON into this frame).
/// </summary>
public sealed record MedicationAdministrationFrame(
    string PatientIdentifier,
    string? PlacerOrderNumber,
    PharmacyMedication Medication,
    decimal DoseQuantity,
    string DoseUnit,
    string Route,
    DateTime AdministeredAtUtc,
    string AdministeredBy,
    string SendingApplication = "DIALYSIS_PDMS");

/// <summary>
/// IO-free input for <see cref="Hl7V2RgvO15Builder"/> — one give/refusal record. We reuse
/// the pharmacy-give trigger to carry a clinical decline: the give segment names the
/// ordered product and <see cref="Reason"/> rides an NTE refusal note so the receiving
/// pharmacy system records that the dose was prepared-but-not-given.
/// </summary>
public sealed record MedicationGiveFrame(
    string PatientIdentifier,
    string? PlacerOrderNumber,
    PharmacyMedication Medication,
    DateTime GiveAtUtc,
    string RecordedBy,
    string Reason,
    string SendingApplication = "DIALYSIS_PDMS");
