using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.Medications.Domain;

/// <summary>
/// Chairside MAR — one per <c>DialysisSession</c>. Captures every medication a clinician
/// records giving (or declining to give) during the treatment. Opens with the session and
/// closes when the session completes / aborts; closed records still accept retroactive
/// edits via an explicit operator action (audited).
/// </summary>
public sealed class MedicationAdministrationRecord : AggregateRoot<Guid>
{
    private readonly List<MedicationAdministrationEntry> _entries = new();

    private MedicationAdministrationRecord() { }

    public MedicationAdministrationRecord(Guid id, Guid sessionId, Guid patientId, DateTime openedAtUtc)
        : base(id)
    {
        SessionId = sessionId;
        PatientId = patientId;
        OpenedAtUtc = openedAtUtc;
        Status = MarStatus.Open;
    }

    public Guid SessionId { get; private set; }

    public Guid PatientId { get; private set; }

    public DateTime OpenedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public MarStatus Status { get; private set; }

    public IReadOnlyCollection<MedicationAdministrationEntry> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Records a positive administration. The optional <paramref name="relatedOrderId"/>
    /// links back to an <c>HIS.MedicationOrder</c> / <c>EHR.MedicationRequest</c> so the
    /// downstream reconciler can mark the order as administered.
    /// </summary>
    public MedicationAdministrationEntry RecordAdministration(
        Guid entryId,
        MedicationCoding medication,
        Dose dose,
        MedicationRoute route,
        DateTime administeredAtUtc,
        string administeredBySub,
        Guid? relatedOrderId)
    {
        EnsureOpen();
        var entry = MedicationAdministrationEntry.Administered(
            entryId, medication, dose, route, administeredAtUtc, administeredBySub, relatedOrderId);
        _entries.Add(entry);
        RaiseIntegrationEvent(new MedicationAdministeredIntegrationEvent
        {
            EntryId = entryId,
            SessionId = SessionId,
            PatientId = PatientId,
            MedicationCodeSystem = medication.CodeSystem,
            MedicationCode = medication.Code,
            MedicationDisplay = medication.DisplayName,
            DoseQuantity = dose.Quantity,
            DoseUnit = dose.Unit,
            Route = route.ToString(),
            AdministeredAtUtc = administeredAtUtc,
            AdministeredBySub = administeredBySub,
            RelatedOrderId = relatedOrderId,
        });
        return entry;
    }

    /// <summary>
    /// Records a declined dose with the operator-supplied reason (e.g. "patient refused",
    /// "vital-signs unstable"). The downstream order reconciler can choose to surface the
    /// decline on the patient chart.
    /// </summary>
    public MedicationAdministrationEntry RecordDecline(
        Guid entryId,
        MedicationCoding medication,
        Dose dose,
        MedicationRoute route,
        DateTime declinedAtUtc,
        string declinedBySub,
        string reason,
        Guid? relatedOrderId)
    {
        EnsureOpen();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var entry = MedicationAdministrationEntry.Declined(
            entryId, medication, dose, route, declinedAtUtc, declinedBySub, reason, relatedOrderId);
        _entries.Add(entry);
        RaiseIntegrationEvent(new MedicationDeclinedIntegrationEvent
        {
            EntryId = entryId,
            SessionId = SessionId,
            PatientId = PatientId,
            MedicationCodeSystem = medication.CodeSystem,
            MedicationCode = medication.Code,
            DeclinedAtUtc = declinedAtUtc,
            DeclinedBySub = declinedBySub,
            Reason = reason,
            RelatedOrderId = relatedOrderId,
        });
        return entry;
    }

    /// <summary>Closes the MAR. No new entries permitted after close.</summary>
    public void Close(DateTime closedAtUtc)
    {
        if (Status == MarStatus.Closed) return;
        Status = MarStatus.Closed;
        ClosedAtUtc = closedAtUtc;
    }

    private void EnsureOpen()
    {
        if (Status != MarStatus.Open)
            throw new InvalidOperationException(
                $"Cannot mutate a closed MAR. Reopen via the operator override path first.");
    }
}

public enum MarStatus
{
    Open = 0,
    Closed = 1,
}

/// <summary>One entry in the MAR — either an administration or a decline.</summary>
public sealed class MedicationAdministrationEntry
{
    // EF Core materialises owned-type ctor params via property setters rather than the
    // bound ctor, so we provide a private parameterless ctor and private setters on the
    // reference-typed properties. Domain code still constructs via the factory ctor.
    private MedicationAdministrationEntry()
    {
        Medication = null!;
        Dose = null!;
        ActorSub = null!;
    }

    private MedicationAdministrationEntry(
        Guid id,
        MedicationCoding medication,
        Dose dose,
        MedicationRoute route,
        DateTime occurredAtUtc,
        string actorSub,
        bool wasAdministered,
        string? declineReason,
        Guid? relatedOrderId)
    {
        Id = id;
        Medication = medication;
        Dose = dose;
        Route = route;
        OccurredAtUtc = occurredAtUtc;
        ActorSub = actorSub;
        WasAdministered = wasAdministered;
        DeclineReason = declineReason;
        RelatedOrderId = relatedOrderId;
    }

    public Guid Id { get; private set; }
    public MedicationCoding Medication { get; private set; }
    public Dose Dose { get; private set; }
    public MedicationRoute Route { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string ActorSub { get; private set; }
    public bool WasAdministered { get; private set; }
    public string? DeclineReason { get; private set; }
    public Guid? RelatedOrderId { get; private set; }

    internal static MedicationAdministrationEntry Administered(
        Guid id,
        MedicationCoding medication,
        Dose dose,
        MedicationRoute route,
        DateTime administeredAtUtc,
        string administeredBySub,
        Guid? relatedOrderId) =>
        new(id, medication, dose, route, administeredAtUtc, administeredBySub,
            wasAdministered: true, declineReason: null, relatedOrderId);

    internal static MedicationAdministrationEntry Declined(
        Guid id,
        MedicationCoding medication,
        Dose dose,
        MedicationRoute route,
        DateTime declinedAtUtc,
        string declinedBySub,
        string reason,
        Guid? relatedOrderId) =>
        new(id, medication, dose, route, declinedAtUtc, declinedBySub,
            wasAdministered: false, declineReason: reason, relatedOrderId);
}
