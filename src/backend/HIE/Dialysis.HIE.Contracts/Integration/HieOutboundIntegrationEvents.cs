using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIE.Contracts.Integration;

/// <summary>
/// Emitted when a FHIR resource bundle has been successfully delivered to an external partner endpoint.
/// Downstream subscribers (audit, billing, analytics) can react without coupling to the HIE module.
/// </summary>
public sealed record FhirResourceDeliveredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a FHIR resource bundle has been successfully delivered to an external partner endpoint.
    /// Downstream subscribers (audit, billing, analytics) can react without coupling to the HIE module.
    /// </summary>
    public FhirResourceDeliveredIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid OutboundBundleId,
        Guid PatientId,
        string ResourceType,
        string LogicalId,
        string PartnerId,
        DateTime DeliveredAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.OutboundBundleId = OutboundBundleId;
        this.PatientId = PatientId;
        this.ResourceType = ResourceType;
        this.LogicalId = LogicalId;
        this.PartnerId = PartnerId;
        this.DeliveredAtUtc = DeliveredAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid OutboundBundleId { get; init; }
    public Guid PatientId { get; init; }
    public string ResourceType { get; init; }
    public string LogicalId { get; init; }
    public string PartnerId { get; init; }
    public DateTime DeliveredAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid OutboundBundleId, out Guid PatientId, out string ResourceType, out string LogicalId, out string PartnerId, out DateTime DeliveredAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        OutboundBundleId = this.OutboundBundleId;
        PatientId = this.PatientId;
        ResourceType = this.ResourceType;
        LogicalId = this.LogicalId;
        PartnerId = this.PartnerId;
        DeliveredAtUtc = this.DeliveredAtUtc;
    }
}

/// <summary>
/// Emitted when a FHIR resource bundle has failed delivery after exhausting the retry policy.
/// Operations team should investigate (peer endpoint outage, schema rejection, consent revocation).
/// </summary>
public sealed record FhirResourceDeliveryFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a FHIR resource bundle has failed delivery after exhausting the retry policy.
    /// Operations team should investigate (peer endpoint outage, schema rejection, consent revocation).
    /// </summary>
    public FhirResourceDeliveryFailedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid OutboundBundleId,
        Guid PatientId,
        string ResourceType,
        string PartnerId,
        int Attempts,
        string FailureReason)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.OutboundBundleId = OutboundBundleId;
        this.PatientId = PatientId;
        this.ResourceType = ResourceType;
        this.PartnerId = PartnerId;
        this.Attempts = Attempts;
        this.FailureReason = FailureReason;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid OutboundBundleId { get; init; }
    public Guid PatientId { get; init; }
    public string ResourceType { get; init; }
    public string PartnerId { get; init; }
    public int Attempts { get; init; }
    public string FailureReason { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid OutboundBundleId, out Guid PatientId, out string ResourceType, out string PartnerId, out int Attempts, out string FailureReason)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        OutboundBundleId = this.OutboundBundleId;
        PatientId = this.PatientId;
        ResourceType = this.ResourceType;
        PartnerId = this.PartnerId;
        Attempts = this.Attempts;
        FailureReason = this.FailureReason;
    }
}
