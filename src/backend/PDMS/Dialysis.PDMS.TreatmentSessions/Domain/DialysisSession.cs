using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Contracts.Integration;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

public enum DialysisSessionStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Aborted = 4,
    Cancelled = 5,
    Paused = 6,
}

public sealed class DialysisSession : AggregateRoot<Guid>
{
    private readonly List<IntradialyticReading> _readings = new();

    private DialysisSession()
    {
    }

    public DialysisSession(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public DateTime ScheduledStartUtc { get; private set; }

    public DateTime? ActualStartUtc { get; private set; }

    public DateTime? ActualEndUtc { get; private set; }

    public DialysisSessionStatus Status { get; private set; }

    public SessionPrescription Prescription { get; private set; } = null!;

    public VascularAccess Access { get; private set; } = null!;

    public decimal? AchievedUfVolumeLiters { get; private set; }

    public string? AbortReasonCode { get; private set; }

    /// <summary>The dialysis machine currently bound to this session. Set via <see cref="BindMachine"/> at start time; remains for the lifetime of the session.</summary>
    public Guid? MachineId { get; private set; }

    public IReadOnlyCollection<IntradialyticReading> Readings => _readings;

    public static DialysisSession Schedule(
        Guid id,
        Guid patientId,
        DateTime scheduledStartUtc,
        SessionPrescription prescription,
        VascularAccess access)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentNullException.ThrowIfNull(prescription);
        ArgumentNullException.ThrowIfNull(access);
        if (scheduledStartUtc < DateTime.UtcNow.AddHours(-1))
            throw new ArgumentException("Cannot schedule a session in the past.", nameof(scheduledStartUtc));

        return new DialysisSession(id)
        {
            PatientId = patientId,
            ScheduledStartUtc = scheduledStartUtc,
            Status = DialysisSessionStatus.Scheduled,
            Prescription = prescription,
            Access = access,
        };
    }

    public void Start(DateTime startedAtUtc)
    {
        if (Status != DialysisSessionStatus.Scheduled)
            throw new InvalidOperationException($"Cannot start a session in status {Status}.");
        Status = DialysisSessionStatus.InProgress;
        ActualStartUtc = startedAtUtc;

        RaiseIntegrationEvent(new DialysisSessionStartedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SessionId: Id,
            PatientId: PatientId,
            StartedAtUtc: startedAtUtc,
            DialyzerModel: Prescription.DialyzerModel,
            BloodFlowRateMlPerMin: Prescription.BloodFlowRateMlPerMin));
    }

    public IntradialyticReading RecordReading(
        DateTime observedAtUtc,
        int systolic,
        int diastolic,
        int heartRateBpm,
        decimal arterialPressureMmHg,
        decimal venousPressureMmHg,
        decimal ultrafiltrationRateMlPerHour,
        decimal conductivityMsPerCm,
        string? notes = null,
        Guid? explicitReadingId = null)
    {
        if (Status != DialysisSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot record reading on session in status {Status}.");

        // explicitReadingId carries the durable-bus CommandId so a redelivery of the same
        // envelope yields the same reading row. The caller (durable consumer) hands the
        // CommandId in; for sync callers it stays null and we generate a fresh v7 guid.
        var readingId = explicitReadingId is { } supplied && supplied != Guid.Empty
            ? supplied
            : Guid.CreateVersion7();
        var reading = IntradialyticReading.Record(
            readingId, Id, observedAtUtc,
            systolic, diastolic, heartRateBpm,
            arterialPressureMmHg, venousPressureMmHg,
            ultrafiltrationRateMlPerHour, conductivityMsPerCm,
            notes);
        _readings.Add(reading);
        return reading;
    }

    public void Complete(DateTime completedAtUtc, decimal achievedUfVolumeLiters)
    {
        if (Status != DialysisSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete a session in status {Status}.");
        if (!ActualStartUtc.HasValue)
            throw new InvalidOperationException("Session was never started.");
        if (completedAtUtc <= ActualStartUtc.Value)
            throw new ArgumentException("Completion must follow start.", nameof(completedAtUtc));
        if (achievedUfVolumeLiters < 0)
            throw new ArgumentOutOfRangeException(nameof(achievedUfVolumeLiters));

        Status = DialysisSessionStatus.Completed;
        ActualEndUtc = completedAtUtc;
        AchievedUfVolumeLiters = achievedUfVolumeLiters;

        var durationMinutes = (int)Math.Round((completedAtUtc - ActualStartUtc.Value).TotalMinutes);
        RaiseIntegrationEvent(new DialysisSessionCompletedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SessionId: Id,
            PatientId: PatientId,
            CompletedAtUtc: completedAtUtc,
            ActualDurationMinutes: durationMinutes,
            AchievedUfVolumeLiters: achievedUfVolumeLiters));
    }

    public void Abort(DateTime abortedAtUtc, string reasonCode)
    {
        if (Status is DialysisSessionStatus.Completed or DialysisSessionStatus.Aborted or DialysisSessionStatus.Cancelled)
            throw new InvalidOperationException($"Cannot abort a session in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        Status = DialysisSessionStatus.Aborted;
        ActualEndUtc = abortedAtUtc;
        AbortReasonCode = reasonCode.Trim();

        RaiseIntegrationEvent(new DialysisSessionAbortedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SessionId: Id,
            PatientId: PatientId,
            AbortedAtUtc: abortedAtUtc,
            ReasonCode: AbortReasonCode));
    }

    /// <summary>
    /// Associates a physical machine with this session. Idempotent when the same machine is re-bound; rejects
    /// a different machine on an in-progress session to prevent telemetry cross-contamination.
    /// </summary>
    public void BindMachine(Guid machineId)
    {
        if (machineId == Guid.Empty) throw new ArgumentException("Machine required.", nameof(machineId));
        if (MachineId.HasValue && MachineId.Value != machineId)
            throw new InvalidOperationException("Session is already bound to a different machine.");
        MachineId = machineId;
    }

    /// <summary>
    /// Records that the bound machine emitted a treatment observation. PDMS persists the <see cref="TreatmentObservation"/>
    /// row in its own table (high-volume child of the session). Domain-level invariant: session must be in progress and
    /// the machine must match.
    /// </summary>
    public void ReceiveObservation(Guid machineId, DateTime observedAtUtc)
    {
        if (Status != DialysisSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot ingest observation on session in status {Status}.");
        if (MachineId is null)
            throw new InvalidOperationException("Session has no bound machine.");
        if (MachineId.Value != machineId)
            throw new InvalidOperationException("Observation came from a different machine than the one bound to this session.");
        if (observedAtUtc < ActualStartUtc)
            throw new ArgumentException("Observation predates session start.", nameof(observedAtUtc));
    }

    /// <summary>
    /// Records that the bound machine raised an alarm. Domain method is invariant-checking only; the
    /// <see cref="TreatmentAlarm"/> aggregate owns the alarm-state lifecycle.
    /// </summary>
    public void RecordAlarm(Guid machineId, DateTime observedAtUtc)
    {
        if (Status is DialysisSessionStatus.Completed or DialysisSessionStatus.Cancelled)
            throw new InvalidOperationException($"Cannot record alarm on session in status {Status}.");
        if (MachineId is null)
            throw new InvalidOperationException("Session has no bound machine.");
        if (MachineId.Value != machineId)
            throw new InvalidOperationException("Alarm came from a different machine than the one bound to this session.");
        if (observedAtUtc < ActualStartUtc)
            throw new ArgumentException("Alarm predates session start.", nameof(observedAtUtc));
    }

    /// <summary>Transitions an in-progress session to paused (e.g. machine entered standby).</summary>
    public void Pause()
    {
        if (Status != DialysisSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot pause a session in status {Status}.");
        Status = DialysisSessionStatus.Paused;
    }

    /// <summary>Resumes a paused session.</summary>
    public void Resume()
    {
        if (Status != DialysisSessionStatus.Paused)
            throw new InvalidOperationException($"Cannot resume a session in status {Status}.");
        Status = DialysisSessionStatus.InProgress;
    }
}
