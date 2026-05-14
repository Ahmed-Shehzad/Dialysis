using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.SmartConnect.Contracts.Integration;

/// <summary>
/// One observation extracted from an HL7 OBX segment of an ORU^R01 message. Vendor-neutral shape: an
/// ISO/IEEE 11073 MDC numeric code, the containment-tree path (e.g. <c>"1.1.4.27.1"</c>) that locates the
/// observation inside the machine's MDS → VMD → Channel → Metric hierarchy, plus exactly one of
/// <see cref="NumericValue"/> / <see cref="StringValue"/> / a profile array pair. Units use UCUM.
/// </summary>
public sealed record NormalizedMachineObservation(
    long MdcCode,
    string ContainmentPath,
    decimal? NumericValue,
    string? StringValue,
    string? Units,
    decimal[]? ProfileValues,
    int[]? ProfileTimesSeconds,
    DateTime ObservedAtUtc);

/// <summary>
/// SmartConnect successfully parsed an ORU^R01 (PCD-01) treatment-status message from a dialysis machine
/// and normalized its OBX payload into a flat list of <see cref="NormalizedMachineObservation"/> entries.
/// Subscribers (PDMS) own persistence and treatment-session association.
/// </summary>
public sealed record DialysisMachineTreatmentSnapshotIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string MachineSerial,
    string? VendorCode,
    string? ModelCode,
    Guid SourceMessageId,
    string MessageControlId,
    DateTime ObservedAtUtc,
    string? PatientMrn,
    string? FillerOrderNumber,
    IReadOnlyList<NormalizedMachineObservation> Observations) : IIntegrationEvent;

public enum DialysisMachineAlarmState
{
    Present = 1,
    Inactivating = 2,
    Resolved = 3,
}

/// <summary>
/// SmartConnect parsed an ORU^R40 (PCD-04) alert message. <see cref="State"/> distinguishes initial activation,
/// keep-alive while still present, inactivating, or final resolution. PDMS uses these transitions to drive
/// its <c>TreatmentAlarm</c> state machine.
/// </summary>
public sealed record DialysisMachineAlarmIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string MachineSerial,
    Guid SourceMessageId,
    string MessageControlId,
    DateTime ObservedAtUtc,
    long AlarmCode,
    string? AlarmSource,
    string? AlarmPhase,
    DialysisMachineAlarmState State,
    IReadOnlyList<NormalizedMachineObservation> AlarmObservations) : IIntegrationEvent;
