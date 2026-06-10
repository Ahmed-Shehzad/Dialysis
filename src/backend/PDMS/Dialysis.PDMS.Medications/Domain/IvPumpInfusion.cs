using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.Medications.Domain;

/// <summary>
/// Single IV pump infusion event. Each programmed dose on the pump becomes one aggregate
/// instance, lifecycle: <see cref="IvPumpStatus.Running"/> → <see cref="IvPumpStatus.Completed"/>
/// (normal) or <see cref="IvPumpStatus.Alarm"/> (operator intervention). The same pump can
/// produce multiple sequential infusions within a single session — they're distinct rows.
/// </summary>
public sealed class IvPumpInfusion : AggregateRoot<Guid>
{
    private IvPumpInfusion() { }

    public IvPumpInfusion(
        Guid id,
        Guid sessionId,
        Guid chairId,
        string pumpDeviceId,
        string vendorCode,
        MedicationCoding? medication,
        decimal programmedRateMlPerHour,
        decimal programmedVolumeMl,
        DateTime startedAtUtc) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pumpDeviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorCode);
        SessionId = sessionId;
        ChairId = chairId;
        PumpDeviceId = pumpDeviceId;
        VendorCode = vendorCode;
        Medication = medication;
        ProgrammedRateMlPerHour = programmedRateMlPerHour;
        ProgrammedVolumeMl = programmedVolumeMl;
        StartedAtUtc = startedAtUtc;
        ActualRateMlPerHour = programmedRateMlPerHour;
        InfusedVolumeMl = 0m;
        Status = IvPumpStatus.Running;

        RaiseIntegrationEvent(new IvPumpInfusionStartedIntegrationEvent
        {
            InfusionId = id,
            SessionId = sessionId,
            PumpDeviceId = pumpDeviceId,
            VendorCode = vendorCode,
            MedicationCodeSystem = medication?.CodeSystem,
            MedicationCode = medication?.Code,
            ProgrammedRateMlPerHour = programmedRateMlPerHour,
            ProgrammedVolumeMl = programmedVolumeMl,
            StartedAtUtc = startedAtUtc,
        });
    }

    public Guid SessionId { get; private set; }
    public Guid ChairId { get; private set; }
    public string PumpDeviceId { get; private set; } = null!;
    public string VendorCode { get; private set; } = null!;
    public MedicationCoding? Medication { get; private set; }
    public decimal ProgrammedRateMlPerHour { get; private set; }
    public decimal ActualRateMlPerHour { get; private set; }
    public decimal ProgrammedVolumeMl { get; private set; }
    public decimal InfusedVolumeMl { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public IvPumpStatus Status { get; private set; }

    /// <summary>Adds a vendor-driver telemetry update.</summary>
    public void RecordReading(decimal actualRateMlPerHour, decimal infusedVolumeMl)
    {
        if (Status is IvPumpStatus.Completed)
            throw new InvalidOperationException("Cannot record reading on a completed infusion.");
        ActualRateMlPerHour = actualRateMlPerHour;
        InfusedVolumeMl = infusedVolumeMl;
    }

    /// <summary>Operator paused the infusion (e.g. flush, hold for vitals check).</summary>
    public void Pause() => Status = IvPumpStatus.Paused;

    /// <summary>Operator resumed after a pause.</summary>
    public void Resume()
    {
        if (Status != IvPumpStatus.Paused)
            throw new InvalidOperationException($"Cannot resume from {Status}.");
        Status = IvPumpStatus.Running;
    }

    /// <summary>Pump raised an alarm.</summary>
    public void MarkAlarm(string alarmCode, string alarmText, IvPumpAlarmSeverity severity, DateTime raisedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alarmCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(alarmText);
        Status = IvPumpStatus.Alarm;
        RaiseIntegrationEvent(new IvPumpAlarmRaisedIntegrationEvent
        {
            InfusionId = Id,
            SessionId = SessionId,
            ChairId = ChairId,
            PumpDeviceId = PumpDeviceId,
            AlarmCode = alarmCode,
            AlarmText = alarmText,
            Severity = severity,
            RaisedAtUtc = raisedAtUtc,
        });
    }

    /// <summary>Infusion completed normally — programmed volume delivered.</summary>
    public void Complete(decimal finalInfusedVolumeMl, DateTime endedAtUtc)
    {
        if (Status == IvPumpStatus.Completed)
            return;
        InfusedVolumeMl = finalInfusedVolumeMl;
        Status = IvPumpStatus.Completed;
        EndedAtUtc = endedAtUtc;
        RaiseIntegrationEvent(new IvPumpInfusionCompletedIntegrationEvent
        {
            InfusionId = Id,
            SessionId = SessionId,
            InfusedVolumeMl = finalInfusedVolumeMl,
            EndedAtUtc = endedAtUtc,
        });
    }
}

public enum IvPumpStatus
{
    Running = 0,
    Paused = 1,
    Completed = 2,
    Alarm = 3,
}
