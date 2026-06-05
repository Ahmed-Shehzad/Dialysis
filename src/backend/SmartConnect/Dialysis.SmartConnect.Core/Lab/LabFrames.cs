namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// A coded laboratory test (CWE triplet: identifier ^ text ^ coding-system). Carries the LOINC
/// code the receiving Laboratory Information System needs to reconcile the requested service
/// against its own compendium.
/// </summary>
public sealed record LabTestRequest(string Code, string Display, string System = "LN");

/// <summary>
/// IO-free input for <see cref="Hl7V2OrmO01Builder"/> and <see cref="LabServiceRequestBuilder"/> —
/// one placed lab order with one or more requested tests. Mirrors the wire-relevant fields of the
/// upstream <c>LabOrderPlacedIntegrationEvent</c> without coupling SmartConnect to the Lab module
/// (the transform stages deserialise the event JSON into this frame).
/// </summary>
public sealed record LabOrderFrame(
    string PatientIdentifier,
    string PlacerOrderNumber,
    bool IsStat,
    string? Specimen,
    IReadOnlyList<LabTestRequest> Tests,
    DateTime OrderedAtUtc,
    string SendingApplication = "DIALYSIS_LAB");
