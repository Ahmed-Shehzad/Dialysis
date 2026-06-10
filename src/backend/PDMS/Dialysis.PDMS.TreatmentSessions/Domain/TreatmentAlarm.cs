using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

public enum TreatmentAlarmState
{
    Present = 1,
    Inactivating = 2,
    Resolved = 3,
}

/// <summary>
/// PDMS-side state of one dialysis-machine alarm. ORU^R40 messages from the machine drive the transitions:
/// initial activation creates the aggregate in <see cref="TreatmentAlarmState.Present"/>; subsequent
/// keep-alives <see cref="Refresh"/> the timestamp; a state-change wire message moves it to
/// <see cref="TreatmentAlarmState.Inactivating"/> and finally <see cref="TreatmentAlarmState.Resolved"/>.
/// Caregiver-side <see cref="Acknowledge"/> is orthogonal — it can happen in any state.
/// </summary>
public sealed class TreatmentAlarm : AggregateRoot<Guid>
{
    private TreatmentAlarm()
    {
    }

    public TreatmentAlarm(Guid id) : base(id)
    {
    }

    public Guid? SessionId { get; private set; }

    public Guid MachineId { get; private set; }

    public long AlarmCode { get; private set; }

    public string? AlarmSource { get; private set; }

    public string? AlarmPhase { get; private set; }

    public TreatmentAlarmState State { get; private set; }

    public DateTime FirstObservedUtc { get; private set; }

    public DateTime LastObservedUtc { get; private set; }

    public DateTime? AcknowledgedUtc { get; private set; }

    public string? AcknowledgedBy { get; private set; }

    public static TreatmentAlarm Raise(
        Guid id,
        Guid? sessionId,
        Guid machineId,
        long alarmCode,
        string? alarmSource,
        string? alarmPhase,
        DateTime observedAtUtc)
    {
        if (machineId == Guid.Empty)
            throw new ArgumentException("Machine required.", nameof(machineId));
        if (alarmCode <= 0)
            throw new ArgumentOutOfRangeException(nameof(alarmCode));

        return new TreatmentAlarm(id)
        {
            SessionId = sessionId,
            MachineId = machineId,
            AlarmCode = alarmCode,
            AlarmSource = string.IsNullOrWhiteSpace(alarmSource) ? null : alarmSource.Trim(),
            AlarmPhase = string.IsNullOrWhiteSpace(alarmPhase) ? null : alarmPhase.Trim(),
            State = TreatmentAlarmState.Present,
            FirstObservedUtc = observedAtUtc,
            LastObservedUtc = observedAtUtc,
        };
    }

    public void Refresh(DateTime observedAtUtc)
    {
        if (State == TreatmentAlarmState.Resolved)
            throw new InvalidOperationException("Cannot refresh a resolved alarm.");
        if (observedAtUtc < LastObservedUtc)
            return;
        LastObservedUtc = observedAtUtc;
    }

    public void MarkInactivating(DateTime observedAtUtc)
    {
        if (State == TreatmentAlarmState.Resolved)
            throw new InvalidOperationException("Cannot transition a resolved alarm.");
        State = TreatmentAlarmState.Inactivating;
        if (observedAtUtc > LastObservedUtc)
            LastObservedUtc = observedAtUtc;
    }

    public void MarkResolved(DateTime observedAtUtc)
    {
        State = TreatmentAlarmState.Resolved;
        if (observedAtUtc > LastObservedUtc)
            LastObservedUtc = observedAtUtc;
    }

    public void Acknowledge(DateTime acknowledgedAtUtc, string acknowledgedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(acknowledgedBy);
        if (AcknowledgedUtc.HasValue)
            return;
        AcknowledgedUtc = acknowledgedAtUtc;
        AcknowledgedBy = acknowledgedBy.Trim();
    }
}
