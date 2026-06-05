namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// One resulted observation parsed from an inbound ORU OBX segment. <see cref="Interpretation"/>
/// carries the HL7 table-0078 abnormal flag (e.g. <c>N</c>, <c>L</c>, <c>H</c>, <c>LL</c>,
/// <c>HH</c>, <c>A</c>); <see cref="ReferenceRange"/> is the OBX-7 reference interval verbatim.
/// </summary>
public sealed record LabResultObservation(
    string Code,
    string Display,
    string Value,
    string? Unit,
    string? ReferenceRange,
    string? Interpretation);

/// <summary>
/// Transport-neutral projection of an inbound HL7 v2 <c>ORU^R01</c> (or FHIR <c>Observation</c>
/// bundle) result, matched back to the placing order by <see cref="PlacerOrderNumber"/>. SmartConnect
/// produces this from the wire; the bus bridge maps it onto the Lab-owned
/// <c>LabResultReceivedIntegrationEvent</c>. <see cref="IsFinal"/> distinguishes a final result
/// (OBX-11 <c>F</c>) from a preliminary/partial one.
/// </summary>
public sealed record LabResultFrame(
    string PatientIdentifier,
    string PlacerOrderNumber,
    string? FillerOrderNumber,
    bool IsFinal,
    IReadOnlyList<LabResultObservation> Observations,
    DateTime ResultedAtUtc);
