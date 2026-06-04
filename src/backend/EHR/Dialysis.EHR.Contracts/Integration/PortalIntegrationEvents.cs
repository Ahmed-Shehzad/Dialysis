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
