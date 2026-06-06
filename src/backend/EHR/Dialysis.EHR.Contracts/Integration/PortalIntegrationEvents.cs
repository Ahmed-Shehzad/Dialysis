using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record PatientPortalAppointmentRequestedIntegrationEvent : IIntegrationEvent
{
    public PatientPortalAppointmentRequestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid RequestId,
        Guid PatientId,
        string ReasonText,
        DateTime EarliestPreferredUtc,
        DateTime LatestPreferredUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.RequestId = RequestId;
        this.PatientId = PatientId;
        this.ReasonText = ReasonText;
        this.EarliestPreferredUtc = EarliestPreferredUtc;
        this.LatestPreferredUtc = LatestPreferredUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid RequestId { get; init; }
    public Guid PatientId { get; init; }
    public string ReasonText { get; init; }
    public DateTime EarliestPreferredUtc { get; init; }
    public DateTime LatestPreferredUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid RequestId, out Guid PatientId, out string ReasonText, out DateTime EarliestPreferredUtc, out DateTime LatestPreferredUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        RequestId = this.RequestId;
        PatientId = this.PatientId;
        ReasonText = this.ReasonText;
        EarliestPreferredUtc = this.EarliestPreferredUtc;
        LatestPreferredUtc = this.LatestPreferredUtc;
    }
}

public sealed record PatientPortalSecureMessageSentIntegrationEvent : IIntegrationEvent
{
    public PatientPortalSecureMessageSentIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid MessageId,
        Guid PatientId,
        Guid? TargetProviderId,
        string Subject)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.MessageId = MessageId;
        this.PatientId = PatientId;
        this.TargetProviderId = TargetProviderId;
        this.Subject = Subject;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid MessageId { get; init; }
    public Guid PatientId { get; init; }
    public Guid? TargetProviderId { get; init; }
    public string Subject { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid MessageId, out Guid PatientId, out Guid? TargetProviderId, out string Subject)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        MessageId = this.MessageId;
        PatientId = this.PatientId;
        TargetProviderId = this.TargetProviderId;
        Subject = this.Subject;
    }
}

/// <summary>
/// Raised when a clinician publishes an after-visit summary to the patient portal. Drives the
/// patient-facing toast. Metadata only — the SPA refetches the summary through the synchronous API.
/// </summary>
public sealed record AfterVisitSummaryPublishedIntegrationEvent : IIntegrationEvent
{
    public AfterVisitSummaryPublishedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SummaryId,
        Guid PatientId,
        DateTime VisitDateUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SummaryId = SummaryId;
        this.PatientId = PatientId;
        this.VisitDateUtc = VisitDateUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SummaryId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime VisitDateUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SummaryId, out Guid PatientId, out DateTime VisitDateUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SummaryId = this.SummaryId;
        PatientId = this.PatientId;
        VisitDateUtc = this.VisitDateUtc;
    }
}

/// <summary>
/// Raised when staff resolve a patient's appointment request (approve → an appointment was booked, or
/// decline). Drives the patient-facing portal toast. Metadata only — the SPA refetches via the API.
/// </summary>
public sealed record PatientPortalAppointmentResolvedIntegrationEvent : IIntegrationEvent
{
    public PatientPortalAppointmentResolvedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid RequestId,
        Guid PatientId,
        bool Approved,
        Guid? CreatedAppointmentId,
        string? StaffNote)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.RequestId = RequestId;
        this.PatientId = PatientId;
        this.Approved = Approved;
        this.CreatedAppointmentId = CreatedAppointmentId;
        this.StaffNote = StaffNote;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid RequestId { get; init; }
    public Guid PatientId { get; init; }
    public bool Approved { get; init; }
    public Guid? CreatedAppointmentId { get; init; }
    public string? StaffNote { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid RequestId, out Guid PatientId, out bool Approved, out Guid? CreatedAppointmentId, out string? StaffNote)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        RequestId = this.RequestId;
        PatientId = this.PatientId;
        Approved = this.Approved;
        CreatedAppointmentId = this.CreatedAppointmentId;
        StaffNote = this.StaffNote;
    }
}

/// <summary>
/// Raised when a provider replies to a patient on a secure-message thread. Drives the patient-facing
/// portal toast ("your care team replied"). Metadata only — the SPA refetches the thread through the
/// synchronous, permission-checked API.
/// </summary>
public sealed record PatientPortalSecureMessageReceivedIntegrationEvent : IIntegrationEvent
{
    public PatientPortalSecureMessageReceivedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid MessageId,
        Guid PatientId,
        Guid ThreadId,
        string Subject)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.MessageId = MessageId;
        this.PatientId = PatientId;
        this.ThreadId = ThreadId;
        this.Subject = Subject;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid MessageId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ThreadId { get; init; }
    public string Subject { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid MessageId, out Guid PatientId, out Guid ThreadId, out string Subject)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        MessageId = this.MessageId;
        PatientId = this.PatientId;
        ThreadId = this.ThreadId;
        Subject = this.Subject;
    }
}
