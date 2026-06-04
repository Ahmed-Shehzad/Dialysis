using Dialysis.SmartConnect.Contracts.Integration;

namespace Dialysis.PDMS.TreatmentSessions.Adapters;

/// <summary>
/// Anticorruption Layer (Evans pp. 258–260) at the SmartConnect ↔ PDMS boundary for treatment-status
/// snapshots. Translates the upstream <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/>
/// into a PDMS-local <see cref="IncomingTreatmentSnapshot"/> intent.
/// </summary>
public static class SmartConnectSnapshotTranslator
{
    public static IncomingTreatmentSnapshot Translate(DialysisMachineTreatmentSnapshotIntegrationEvent message) =>
        new(
            MachineSerial: message.MachineSerial,
            VendorCode: message.VendorCode,
            ModelCode: message.ModelCode,
            ObservedAtUtc: message.ObservedAtUtc,
            PatientMrn: message.PatientMrn,
            FillerOrderNumber: message.FillerOrderNumber,
            Observations: [.. message.Observations.Select(o => new IncomingObservation(
                MdcCode: o.MdcCode,
                ContainmentPath: o.ContainmentPath,
                NumericValue: o.NumericValue,
                StringValue: o.StringValue,
                Units: o.Units,
                ObservedAtUtc: o.ObservedAtUtc))],
            MessageControlId: message.MessageControlId);
}

public sealed record IncomingTreatmentSnapshot
{
    public IncomingTreatmentSnapshot(string MachineSerial,
        string? VendorCode,
        string? ModelCode,
        DateTime ObservedAtUtc,
        string? PatientMrn,
        string? FillerOrderNumber,
        IReadOnlyList<IncomingObservation> Observations,
        string MessageControlId)
    {
        this.MachineSerial = MachineSerial;
        this.VendorCode = VendorCode;
        this.ModelCode = ModelCode;
        this.ObservedAtUtc = ObservedAtUtc;
        this.PatientMrn = PatientMrn;
        this.FillerOrderNumber = FillerOrderNumber;
        this.Observations = Observations;
        this.MessageControlId = MessageControlId;
    }
    public string MachineSerial { get; init; }
    public string? VendorCode { get; init; }
    public string? ModelCode { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public string? PatientMrn { get; init; }
    public string? FillerOrderNumber { get; init; }
    public IReadOnlyList<IncomingObservation> Observations { get; init; }
    public string MessageControlId { get; init; }
    public void Deconstruct(out string MachineSerial, out string? VendorCode, out string? ModelCode, out DateTime ObservedAtUtc, out string? PatientMrn, out string? FillerOrderNumber, out IReadOnlyList<IncomingObservation> Observations, out string MessageControlId)
    {
        MachineSerial = this.MachineSerial;
        VendorCode = this.VendorCode;
        ModelCode = this.ModelCode;
        ObservedAtUtc = this.ObservedAtUtc;
        PatientMrn = this.PatientMrn;
        FillerOrderNumber = this.FillerOrderNumber;
        Observations = this.Observations;
        MessageControlId = this.MessageControlId;
    }
}

public sealed record IncomingObservation
{
    public IncomingObservation(long MdcCode,
        string ContainmentPath,
        decimal? NumericValue,
        string? StringValue,
        string? Units,
        DateTime ObservedAtUtc)
    {
        this.MdcCode = MdcCode;
        this.ContainmentPath = ContainmentPath;
        this.NumericValue = NumericValue;
        this.StringValue = StringValue;
        this.Units = Units;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public long MdcCode { get; init; }
    public string ContainmentPath { get; init; }
    public decimal? NumericValue { get; init; }
    public string? StringValue { get; init; }
    public string? Units { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out long MdcCode, out string ContainmentPath, out decimal? NumericValue, out string? StringValue, out string? Units, out DateTime ObservedAtUtc)
    {
        MdcCode = this.MdcCode;
        ContainmentPath = this.ContainmentPath;
        NumericValue = this.NumericValue;
        StringValue = this.StringValue;
        Units = this.Units;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}
