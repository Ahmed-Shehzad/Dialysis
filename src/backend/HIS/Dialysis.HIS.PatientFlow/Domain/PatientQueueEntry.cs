namespace Dialysis.HIS.PatientFlow.Domain;

/// <summary>
/// One row of the receptionist's "Today" queue. A patient flows
/// Expected -> Waiting (after check-in) -> InTreatment (after a chair is assigned).
/// Walk-ins skip Expected — there was no appointment to expect.
/// </summary>
/// <remarks>
/// Modelled as a simple entity rather than a full DDD aggregate while the workflow is being
/// shaped UI-first with clinical staff. The repository owns identity allocation; behaviour
/// methods on the entity validate state transitions so handlers stay thin.
/// </remarks>
public sealed class PatientQueueEntry
{
    private PatientQueueEntry(
        Guid id,
        Guid patientId,
        string patientName,
        string mrn,
        DateTime scheduledForUtc,
        QueueStatus status,
        bool eligibilityVerified,
        string? chair)
    {
        Id = id;
        PatientId = patientId;
        PatientName = patientName;
        Mrn = mrn;
        ScheduledForUtc = scheduledForUtc;
        Status = status;
        EligibilityVerified = eligibilityVerified;
        Chair = chair;
    }

    public Guid Id { get; }
    public Guid PatientId { get; }
    public string PatientName { get; private set; }
    public string Mrn { get; private set; }
    public DateTime ScheduledForUtc { get; }
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
        return new PatientQueueEntry(
            id, patientId, patientName, mrn, arrivalUtc,
            QueueStatus.Waiting, eligibilityVerified, chair: null);
    }

    /// <summary>Move an Expected patient into Waiting.</summary>
    public void CheckIn(bool eligibilityAcknowledged)
    {
        if (Status != QueueStatus.Expected)
            throw new InvalidOperationException("Patient is no longer expected.");
        Status = QueueStatus.Waiting;
        EligibilityVerified = EligibilityVerified || eligibilityAcknowledged;
    }

    /// <summary>Move a Waiting patient into a chair.</summary>
    public void AssignChair(string chair)
    {
        if (Status != QueueStatus.Waiting)
            throw new InvalidOperationException("Patient is not waiting for a chair right now.");
        if (string.IsNullOrWhiteSpace(chair))
            throw new ArgumentException("Chair is required.", nameof(chair));
        Status = QueueStatus.InTreatment;
        Chair = chair;
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
