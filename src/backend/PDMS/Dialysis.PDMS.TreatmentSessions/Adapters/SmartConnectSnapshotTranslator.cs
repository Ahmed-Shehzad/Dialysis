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

public sealed record IncomingTreatmentSnapshot(
    string MachineSerial,
    string? VendorCode,
    string? ModelCode,
    DateTime ObservedAtUtc,
    string? PatientMrn,
    string? FillerOrderNumber,
    IReadOnlyList<IncomingObservation> Observations,
    string MessageControlId);

public sealed record IncomingObservation(
    long MdcCode,
    string ContainmentPath,
    decimal? NumericValue,
    string? StringValue,
    string? Units,
    DateTime ObservedAtUtc);
