using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.Medications.IvPumps;

/// <summary>
/// Vendor-neutral normalisation contract. Each vendor's wire format (BD Alaris CQI / Baxter
/// SIGMA / Hospira Plum 360) has its own parser implementing this interface; the inbound HTTP
/// endpoint dispatches by <see cref="VendorCode"/> and the result is a unified
/// <see cref="IvPumpReading"/> the domain consumes regardless of source.
///
/// Each driver's <see cref="ParseAsync"/> deals only with parsing — domain operations
/// (start / record reading / complete / alarm) happen on the <see cref="IvPumpInfusion"/>
/// aggregate after the dispatch path resolves which infusion the reading belongs to.
/// </summary>
public interface IIvPumpDriver
{
    /// <summary>Stable vendor key — e.g. <c>"bd-alaris"</c>, <c>"baxter-sigma"</c>, <c>"plum-360"</c>, <c>"pcd04"</c>.</summary>
    string VendorCode { get; }

    /// <summary>Parses a vendor-shaped payload into the unified reading. Throws on malformed input.</summary>
    Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}

/// <summary>
/// Unified shape produced by every vendor driver. The dispatch path correlates
/// (<see cref="PumpDeviceId"/>, <see cref="SequenceNumber"/>) to either start a new
/// <see cref="IvPumpInfusion"/> or update an existing one.
/// </summary>
public sealed record IvPumpReading
{
    /// <summary>
    /// Unified shape produced by every vendor driver. The dispatch path correlates
    /// (<see cref="PumpDeviceId"/>, <see cref="SequenceNumber"/>) to either start a new
    /// <see cref="IvPumpInfusion"/> or update an existing one.
    /// </summary>
    public IvPumpReading(string VendorCode,
        string PumpDeviceId,
        long SequenceNumber,
        IvPumpReadingKind Kind,
        DateTime CapturedAtUtc,
        decimal? ProgrammedRateMlPerHour,
        decimal? ActualRateMlPerHour,
        decimal? ProgrammedVolumeMl,
        decimal? InfusedVolumeMl,
        string? MedicationCodeSystem,
        string? MedicationCode,
        string? AlarmCode,
        string? AlarmText,
        IvPumpAlarmSeverity? AlarmSeverity)
    {
        this.VendorCode = VendorCode;
        this.PumpDeviceId = PumpDeviceId;
        this.SequenceNumber = SequenceNumber;
        this.Kind = Kind;
        this.CapturedAtUtc = CapturedAtUtc;
        this.ProgrammedRateMlPerHour = ProgrammedRateMlPerHour;
        this.ActualRateMlPerHour = ActualRateMlPerHour;
        this.ProgrammedVolumeMl = ProgrammedVolumeMl;
        this.InfusedVolumeMl = InfusedVolumeMl;
        this.MedicationCodeSystem = MedicationCodeSystem;
        this.MedicationCode = MedicationCode;
        this.AlarmCode = AlarmCode;
        this.AlarmText = AlarmText;
        this.AlarmSeverity = AlarmSeverity;
    }
    public string VendorCode { get; init; }
    public string PumpDeviceId { get; init; }
    public long SequenceNumber { get; init; }
    public IvPumpReadingKind Kind { get; init; }
    public DateTime CapturedAtUtc { get; init; }
    public decimal? ProgrammedRateMlPerHour { get; init; }
    public decimal? ActualRateMlPerHour { get; init; }
    public decimal? ProgrammedVolumeMl { get; init; }
    public decimal? InfusedVolumeMl { get; init; }
    public string? MedicationCodeSystem { get; init; }
    public string? MedicationCode { get; init; }
    public string? AlarmCode { get; init; }
    public string? AlarmText { get; init; }
    public IvPumpAlarmSeverity? AlarmSeverity { get; init; }
    public void Deconstruct(out string VendorCode, out string PumpDeviceId, out long SequenceNumber, out IvPumpReadingKind Kind, out DateTime CapturedAtUtc, out decimal? ProgrammedRateMlPerHour, out decimal? ActualRateMlPerHour, out decimal? ProgrammedVolumeMl, out decimal? InfusedVolumeMl, out string? MedicationCodeSystem, out string? MedicationCode, out string? AlarmCode, out string? AlarmText, out IvPumpAlarmSeverity? AlarmSeverity)
    {
        VendorCode = this.VendorCode;
        PumpDeviceId = this.PumpDeviceId;
        SequenceNumber = this.SequenceNumber;
        Kind = this.Kind;
        CapturedAtUtc = this.CapturedAtUtc;
        ProgrammedRateMlPerHour = this.ProgrammedRateMlPerHour;
        ActualRateMlPerHour = this.ActualRateMlPerHour;
        ProgrammedVolumeMl = this.ProgrammedVolumeMl;
        InfusedVolumeMl = this.InfusedVolumeMl;
        MedicationCodeSystem = this.MedicationCodeSystem;
        MedicationCode = this.MedicationCode;
        AlarmCode = this.AlarmCode;
        AlarmText = this.AlarmText;
        AlarmSeverity = this.AlarmSeverity;
    }
}

public enum IvPumpReadingKind
{
    /// <summary>Initial programming of a new infusion.</summary>
    Start = 0,

    /// <summary>Mid-infusion rate / volume update.</summary>
    Progress = 1,

    /// <summary>Pause or operator-issued hold.</summary>
    Pause = 2,

    /// <summary>Resume after a pause.</summary>
    Resume = 3,

    /// <summary>Pump raised an alarm — see AlarmCode + AlarmSeverity.</summary>
    Alarm = 4,

    /// <summary>Infusion completed normally.</summary>
    Complete = 5,
}
