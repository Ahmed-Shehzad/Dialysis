using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

namespace Dialysis.HIS.PatientFlow.Domain;

/// <summary>
/// One row of the receptionist's "Today" queue. A patient flows
/// Expected → Waiting (after check-in) → InTreatment (after a chair is assigned).
/// Walk-ins skip Expected — there was no appointment to expect.
/// </summary>
/// <remarks>
/// Proper <see cref="AggregateRoot{TId}"/>: invariants are protected by behaviour
/// methods (no public setters), and state transitions raise integration events from
/// within the same method that mutates state — so a missed event is not possible.
/// The handlers pull the raised events from <see cref="IntegrationEvents"/>, push them
/// to the outbox, and call <see cref="AggregateRoot{TId}.ClearIntegrationEvents"/>.
/// </remarks>
public sealed class PatientQueueEntry : AggregateRoot<Guid>
{
    // EF requires a parameterless ctor. Domain creation goes through the factories.
    private PatientQueueEntry()
    {
        PatientName = string.Empty;
        Mrn = string.Empty;
    }

    private PatientQueueEntry(
        Guid id,
        Guid patientId,
        string patientName,
        string mrn,
        DateTime scheduledForUtc,
        QueueStatus status,
        bool eligibilityVerified,
        string? chair)
        : base(id)
    {
        PatientId = patientId;
        PatientName = patientName;
        Mrn = mrn;
        ScheduledForUtc = scheduledForUtc;
        Status = status;
        EligibilityVerified = eligibilityVerified;
        Chair = chair;
    }

    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; }
    public string Mrn { get; private set; }
    public DateTime ScheduledForUtc { get; private set; }
    public QueueStatus Status { get; private set; }
    public bool EligibilityVerified { get; private set; }
    public string? Chair { get; private set; }

    /// <summary>Schedule an expected arrival from an upstream appointment.</summary>
    public static PatientQueueEntry Schedule(
        Guid id,
        Guid patientId,
        string patientName,
        string mrn,
        DateTime scheduledForUtc,
        bool eligibilityVerified)
    {
        Guard(patientName, mrn);
        return new PatientQueueEntry(
            id, patientId, patientName, mrn, scheduledForUtc,
            QueueStatus.Expected, eligibilityVerified, chair: null);
    }

    /// <summary>Register an unannounced arrival. Walk-ins land directly in Waiting.</summary>
    public static PatientQueueEntry WalkIn(
        Guid id,
        Guid patientId,
        string patientName,
        string mrn,
        DateTime arrivalUtc,
        bool eligibilityVerified)
    {
        Guard(patientName, mrn);
        var entry = new PatientQueueEntry(
            id, patientId, patientName, mrn, arrivalUtc,
            QueueStatus.Waiting, eligibilityVerified, chair: null);
        entry.RaiseIntegrationEvent(new WalkInRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: arrivalUtc,
            SchemaVersion: 1,
            EntryId: entry.Id,
            PatientId: entry.PatientId,
            PatientName: entry.PatientName,
            Mrn: entry.Mrn,
            EligibilityVerified: entry.EligibilityVerified,
            RegisteredAtUtc: arrivalUtc));
        return entry;
    }

    /// <summary>Move an Expected patient into Waiting.</summary>
    public void CheckIn(DateTime arrivalAtUtc, bool eligibilityAcknowledged)
    {
        if (Status != QueueStatus.Expected)
            throw new InvalidOperationException("Patient is no longer expected.");
        Status = QueueStatus.Waiting;
        EligibilityVerified = EligibilityVerified || eligibilityAcknowledged;
        RaiseIntegrationEvent(new PatientCheckedInIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: arrivalAtUtc,
            SchemaVersion: 1,
            EntryId: Id,
            PatientId: PatientId,
            PatientName: PatientName,
            Mrn: Mrn,
            CheckedInAtUtc: arrivalAtUtc));
    }

    /// <summary>Move a Waiting patient into a chair.</summary>
    public void AssignChair(string chair, DateTime placedAtUtc)
    {
        if (Status != QueueStatus.Waiting)
            throw new InvalidOperationException("Patient is not waiting for a chair right now.");
        if (string.IsNullOrWhiteSpace(chair))
            throw new ArgumentException("Chair is required.", nameof(chair));
        Status = QueueStatus.InTreatment;
        Chair = chair;
        RaiseIntegrationEvent(new PatientPlacedInChairIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: placedAtUtc,
            SchemaVersion: 1,
            EntryId: Id,
            PatientId: PatientId,
            Chair: chair,
            PlacedAtUtc: placedAtUtc));
    }

    private static void Guard(string patientName, string mrn)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("Patient name is required.", nameof(patientName));
        if (string.IsNullOrWhiteSpace(mrn))
            throw new ArgumentException("MRN is required.", nameof(mrn));
    }
}

public enum QueueStatus
{
    Expected = 0,
    Waiting = 1,
    InTreatment = 2,
}
