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

    public SessionPrescription Prescription { get; private set; } = default!;

    public VascularAccess Access { get; private set; } = default!;

    public decimal? AchievedUfVolumeLiters { get; private set; }

    public string? AbortReasonCode { get; private set; }

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
        string? notes = null)
    {
        if (Status != DialysisSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot record reading on session in status {Status}.");

        var reading = IntradialyticReading.Record(
            Guid.CreateVersion7(), Id, observedAtUtc,
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
            SessionId: Id,
            PatientId: PatientId,
            AbortedAtUtc: abortedAtUtc,
            ReasonCode: AbortReasonCode));
    }
}
