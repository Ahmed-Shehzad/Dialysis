namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// Identity of the dialysis machine sending a PCD-01 treatment report. Lands in MSH-3
/// (sending application CWE) and OBR-3 (placer order number CWE) per IG §6.2 samples.
/// </summary>
public sealed record MachineIdentity
{
    /// <summary>
    /// Identity of the dialysis machine sending a PCD-01 treatment report. Lands in MSH-3
    /// (sending application CWE) and OBR-3 (placer order number CWE) per IG §6.2 samples.
    /// </summary>
    public MachineIdentity(string ApplicationName,
        string DeviceIdentifier,
        string IdentifierAssigningAuthority)
    {
        this.ApplicationName = ApplicationName;
        this.DeviceIdentifier = DeviceIdentifier;
        this.IdentifierAssigningAuthority = IdentifierAssigningAuthority;
    }
    public string ApplicationName { get; init; }
    public string DeviceIdentifier { get; init; }
    public string IdentifierAssigningAuthority { get; init; }
    public void Deconstruct(out string ApplicationName, out string DeviceIdentifier, out string IdentifierAssigningAuthority)
    {
        ApplicationName = this.ApplicationName;
        DeviceIdentifier = this.DeviceIdentifier;
        IdentifierAssigningAuthority = this.IdentifierAssigningAuthority;
    }
}

/// <summary>
/// Full input model for the PCD-01 treatment report wire builder. One frame =
/// one ORU^R40^ORU_R40 message: machine identity + patient + observed-at +
/// the list of <see cref="ObservationFrame"/> rows to emit.
/// </summary>
public sealed record TreatmentReportFrame
{
    /// <summary>
    /// Full input model for the PCD-01 treatment report wire builder. One frame =
    /// one ORU^R40^ORU_R40 message: machine identity + patient + observed-at +
    /// the list of <see cref="ObservationFrame"/> rows to emit.
    /// </summary>
    public TreatmentReportFrame(MachineIdentity Machine,
        string PatientIdentifier,
        DateTime ObservedAtUtc,
        IReadOnlyList<ObservationFrame> Observations)
    {
        this.Machine = Machine;
        this.PatientIdentifier = PatientIdentifier;
        this.ObservedAtUtc = ObservedAtUtc;
        this.Observations = Observations;
    }
    public MachineIdentity Machine { get; init; }
    public string PatientIdentifier { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public IReadOnlyList<ObservationFrame> Observations { get; init; }
    public void Deconstruct(out MachineIdentity Machine, out string PatientIdentifier, out DateTime ObservedAtUtc, out IReadOnlyList<ObservationFrame> Observations)
    {
        Machine = this.Machine;
        PatientIdentifier = this.PatientIdentifier;
        ObservedAtUtc = this.ObservedAtUtc;
        Observations = this.Observations;
    }
}
