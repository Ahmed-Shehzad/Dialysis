using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.SmartConnect.Contracts.Integration;

/// <summary>
/// One observation extracted from an HL7 OBX segment of an ORU^R01 message. Vendor-neutral shape: an
/// ISO/IEEE 11073 MDC numeric code, the containment-tree path (e.g. <c>"1.1.4.27.1"</c>) that locates the
/// observation inside the machine's MDS → VMD → Channel → Metric hierarchy, plus exactly one of
/// <see cref="NumericValue"/> / <see cref="StringValue"/> / a profile array pair. Units use UCUM.
/// </summary>
public sealed record NormalizedMachineObservation
{
    /// <summary>
    /// One observation extracted from an HL7 OBX segment of an ORU^R01 message. Vendor-neutral shape: an
    /// ISO/IEEE 11073 MDC numeric code, the containment-tree path (e.g. <c>"1.1.4.27.1"</c>) that locates the
    /// observation inside the machine's MDS → VMD → Channel → Metric hierarchy, plus exactly one of
    /// <see cref="NumericValue"/> / <see cref="StringValue"/> / a profile array pair. Units use UCUM.
    /// </summary>
    public NormalizedMachineObservation(long MdcCode,
        string ContainmentPath,
        decimal? NumericValue,
        string? StringValue,
        string? Units,
        decimal[]? ProfileValues,
        int[]? ProfileTimesSeconds,
        DateTime ObservedAtUtc)
    {
        this.MdcCode = MdcCode;
        this.ContainmentPath = ContainmentPath;
        this.NumericValue = NumericValue;
        this.StringValue = StringValue;
        this.Units = Units;
        this.ProfileValues = ProfileValues;
        this.ProfileTimesSeconds = ProfileTimesSeconds;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public long MdcCode { get; init; }
    public string ContainmentPath { get; init; }
    public decimal? NumericValue { get; init; }
    public string? StringValue { get; init; }
    public string? Units { get; init; }
    public decimal[]? ProfileValues { get; init; }
    public int[]? ProfileTimesSeconds { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out long MdcCode, out string ContainmentPath, out decimal? NumericValue, out string? StringValue, out string? Units, out decimal[]? ProfileValues, out int[]? ProfileTimesSeconds, out DateTime ObservedAtUtc)
    {
        MdcCode = this.MdcCode;
        ContainmentPath = this.ContainmentPath;
        NumericValue = this.NumericValue;
        StringValue = this.StringValue;
        Units = this.Units;
        ProfileValues = this.ProfileValues;
        ProfileTimesSeconds = this.ProfileTimesSeconds;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}

/// <summary>
/// SmartConnect successfully parsed an ORU^R01 (PCD-01) treatment-status message from a dialysis machine
/// and normalized its OBX payload into a flat list of <see cref="NormalizedMachineObservation"/> entries.
/// Subscribers (PDMS) own persistence and treatment-session association.
/// </summary>
public sealed record DialysisMachineTreatmentSnapshotIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// SmartConnect successfully parsed an ORU^R01 (PCD-01) treatment-status message from a dialysis machine
    /// and normalized its OBX payload into a flat list of <see cref="NormalizedMachineObservation"/> entries.
    /// Subscribers (PDMS) own persistence and treatment-session association.
    /// </summary>
    public DialysisMachineTreatmentSnapshotIntegrationEvent(Guid EventId,
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
        IReadOnlyList<NormalizedMachineObservation> Observations)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.MachineSerial = MachineSerial;
        this.VendorCode = VendorCode;
        this.ModelCode = ModelCode;
        this.SourceMessageId = SourceMessageId;
        this.MessageControlId = MessageControlId;
        this.ObservedAtUtc = ObservedAtUtc;
        this.PatientMrn = PatientMrn;
        this.FillerOrderNumber = FillerOrderNumber;
        this.Observations = Observations;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string MachineSerial { get; init; }
    public string? VendorCode { get; init; }
    public string? ModelCode { get; init; }
    public Guid SourceMessageId { get; init; }
    public string MessageControlId { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public string? PatientMrn { get; init; }
    public string? FillerOrderNumber { get; init; }
    public IReadOnlyList<NormalizedMachineObservation> Observations { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string MachineSerial, out string? VendorCode, out string? ModelCode, out Guid SourceMessageId, out string MessageControlId, out DateTime ObservedAtUtc, out string? PatientMrn, out string? FillerOrderNumber, out IReadOnlyList<NormalizedMachineObservation> Observations)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        MachineSerial = this.MachineSerial;
        VendorCode = this.VendorCode;
        ModelCode = this.ModelCode;
        SourceMessageId = this.SourceMessageId;
        MessageControlId = this.MessageControlId;
        ObservedAtUtc = this.ObservedAtUtc;
        PatientMrn = this.PatientMrn;
        FillerOrderNumber = this.FillerOrderNumber;
        Observations = this.Observations;
    }
}

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
public sealed record DialysisMachineAlarmIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// SmartConnect parsed an ORU^R40 (PCD-04) alert message. <see cref="State"/> distinguishes initial activation,
    /// keep-alive while still present, inactivating, or final resolution. PDMS uses these transitions to drive
    /// its <c>TreatmentAlarm</c> state machine.
    /// </summary>
    public DialysisMachineAlarmIntegrationEvent(Guid EventId,
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
        IReadOnlyList<NormalizedMachineObservation> AlarmObservations)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.MachineSerial = MachineSerial;
        this.SourceMessageId = SourceMessageId;
        this.MessageControlId = MessageControlId;
        this.ObservedAtUtc = ObservedAtUtc;
        this.AlarmCode = AlarmCode;
        this.AlarmSource = AlarmSource;
        this.AlarmPhase = AlarmPhase;
        this.State = State;
        this.AlarmObservations = AlarmObservations;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string MachineSerial { get; init; }
    public Guid SourceMessageId { get; init; }
    public string MessageControlId { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public long AlarmCode { get; init; }
    public string? AlarmSource { get; init; }
    public string? AlarmPhase { get; init; }
    public DialysisMachineAlarmState State { get; init; }
    public IReadOnlyList<NormalizedMachineObservation> AlarmObservations { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string MachineSerial, out Guid SourceMessageId, out string MessageControlId, out DateTime ObservedAtUtc, out long AlarmCode, out string? AlarmSource, out string? AlarmPhase, out DialysisMachineAlarmState State, out IReadOnlyList<NormalizedMachineObservation> AlarmObservations)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        MachineSerial = this.MachineSerial;
        SourceMessageId = this.SourceMessageId;
        MessageControlId = this.MessageControlId;
        ObservedAtUtc = this.ObservedAtUtc;
        AlarmCode = this.AlarmCode;
        AlarmSource = this.AlarmSource;
        AlarmPhase = this.AlarmPhase;
        State = this.State;
        AlarmObservations = this.AlarmObservations;
    }
}
