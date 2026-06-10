using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.OnCall.Domain;

/// <summary>
/// Tracks one alarm-to-clinician escalation cycle. Created when an alarm raises; ticks through
/// the rotation chain until either a clinician acknowledges (terminal Acknowledged state) or
/// the chain is exhausted (terminal Exhausted state — supervisor + on-call manager paged via
/// out-of-band channel).
///
/// Each <see cref="AlarmDispatchAttempt"/> entry is one channel send to one chain link with the
/// outcome. We retain all attempts so the audit page can show the full delivery trail per
/// alarm, which the GDPR audit gate cites as evidence of timely escalation.
/// </summary>
public sealed class AlarmDispatch : AggregateRoot<Guid>
{
    private readonly List<AlarmDispatchAttempt> _attempts = new();

    private AlarmDispatch() { }

    public AlarmDispatch(
        Guid id,
        Guid infusionId,
        Guid sessionId,
        Guid chairId,
        string alarmCode,
        IvPumpAlarmSeverity severity,
        DateTime startedAtUtc,
        Guid rotationId,
        Guid policyId) : base(id)
    {
        InfusionId = infusionId;
        SessionId = sessionId;
        ChairId = chairId;
        AlarmCode = alarmCode;
        Severity = severity;
        StartedAtUtc = startedAtUtc;
        RotationId = rotationId;
        PolicyId = policyId;
        CurrentLinkIndex = 0;
        Status = AlarmDispatchStatus.Pending;
    }

    public Guid InfusionId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ChairId { get; private set; }
    public string AlarmCode { get; private set; } = null!;
    public IvPumpAlarmSeverity Severity { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid RotationId { get; private set; }
    public Guid PolicyId { get; private set; }
    public int CurrentLinkIndex { get; private set; }
    public AlarmDispatchStatus Status { get; private set; }
    public string? AcknowledgedBySub { get; private set; }
    public IReadOnlyCollection<AlarmDispatchAttempt> Attempts => _attempts.AsReadOnly();

    /// <summary>Records the outcome of one send (success or failure) at the current link.</summary>
    public void RecordAttempt(NotificationChannel channel, string address, bool delivered, string? failureReason, DateTime attemptedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        _attempts.Add(new AlarmDispatchAttempt(
            ChainLinkIndex: CurrentLinkIndex,
            Channel: channel,
            Address: address,
            Delivered: delivered,
            FailureReason: failureReason,
            AttemptedAtUtc: attemptedAtUtc));
        if (delivered && Status == AlarmDispatchStatus.Pending)
        {
            Status = AlarmDispatchStatus.AwaitingAcknowledgement;
        }
    }

    /// <summary>Walks the dispatcher to the next chain link.</summary>
    public void EscalateToNextLink()
    {
        if (Status is AlarmDispatchStatus.Acknowledged or AlarmDispatchStatus.Exhausted)
            return;
        CurrentLinkIndex += 1;
    }

    /// <summary>Marks the chain as exhausted; the operator's out-of-band on-call manager takes over.</summary>
    public void MarkExhausted(DateTime atUtc)
    {
        Status = AlarmDispatchStatus.Exhausted;
        ResolvedAtUtc = atUtc;
    }

    /// <summary>A clinician acknowledged the alarm; dispatch closes.</summary>
    public void Acknowledge(string clinicianSub, DateTime atUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clinicianSub);
        if (Status is AlarmDispatchStatus.Acknowledged or AlarmDispatchStatus.Exhausted)
            return;
        AcknowledgedBySub = clinicianSub;
        Status = AlarmDispatchStatus.Acknowledged;
        ResolvedAtUtc = atUtc;
    }
}

public enum AlarmDispatchStatus
{
    Pending = 0,
    AwaitingAcknowledgement = 1,
    Acknowledged = 2,
    Exhausted = 3,
}

public sealed record AlarmDispatchAttempt
{
    public AlarmDispatchAttempt(int ChainLinkIndex,
        NotificationChannel Channel,
        string Address,
        bool Delivered,
        string? FailureReason,
        DateTime AttemptedAtUtc)
    {
        this.ChainLinkIndex = ChainLinkIndex;
        this.Channel = Channel;
        this.Address = Address;
        this.Delivered = Delivered;
        this.FailureReason = FailureReason;
        this.AttemptedAtUtc = AttemptedAtUtc;
    }
    public int ChainLinkIndex { get; init; }
    public NotificationChannel Channel { get; init; }
    public string Address { get; init; }
    public bool Delivered { get; init; }
    public string? FailureReason { get; init; }
    public DateTime AttemptedAtUtc { get; init; }
    public void Deconstruct(out int ChainLinkIndex, out NotificationChannel Channel, out string Address, out bool Delivered, out string? FailureReason, out DateTime AttemptedAtUtc)
    {
        ChainLinkIndex = this.ChainLinkIndex;
        Channel = this.Channel;
        Address = this.Address;
        Delivered = this.Delivered;
        FailureReason = this.FailureReason;
        AttemptedAtUtc = this.AttemptedAtUtc;
    }
}
