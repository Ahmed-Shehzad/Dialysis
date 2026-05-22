namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// Identity of the dialysis machine sending a PCD-01 treatment report. Lands in MSH-3
/// (sending application CWE) and OBR-3 (placer order number CWE) per IG §6.2 samples.
/// </summary>
public sealed record MachineIdentity(
    string ApplicationName,
    string DeviceIdentifier,
    string IdentifierAssigningAuthority);

/// <summary>
/// Full input model for the PCD-01 treatment report wire builder. One frame =
/// one ORU^R40^ORU_R40 message: machine identity + patient + observed-at +
/// the list of <see cref="ObservationFrame"/> rows to emit.
/// </summary>
public sealed record TreatmentReportFrame(
    MachineIdentity Machine,
    string PatientIdentifier,
    DateTime ObservedAtUtc,
    IReadOnlyList<ObservationFrame> Observations);
