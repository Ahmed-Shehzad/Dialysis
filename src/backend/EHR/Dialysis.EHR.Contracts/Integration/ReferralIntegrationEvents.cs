using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>
/// A clinician referred / transferred a patient to an external organisation. HIE consumes this to
/// assemble a Continuity of Care Document (CCD) and push it to the receiving org over Directed
/// Exchange — the transfer-of-care trigger.
/// </summary>
public sealed record ReferralRequestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// A clinician referred / transferred a patient to an external organisation. HIE consumes this to
    /// assemble a Continuity of Care Document (CCD) and push it to the receiving org over Directed
    /// Exchange — the transfer-of-care trigger.
    /// </summary>
    public ReferralRequestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PatientId,
        string DestinationPartnerId,
        Guid ReferringProviderId,
        string? ReferralReason,
        DateTime RequestedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PatientId = PatientId;
        this.DestinationPartnerId = DestinationPartnerId;
        this.ReferringProviderId = ReferringProviderId;
        this.ReferralReason = ReferralReason;
        this.RequestedAtUtc = RequestedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PatientId { get; init; }

    /// <summary>Partner id of the receiving organisation the CCD is pushed to.</summary>
    public string DestinationPartnerId { get; init; }
    public Guid ReferringProviderId { get; init; }
    public string? ReferralReason { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PatientId, out string DestinationPartnerId, out Guid ReferringProviderId, out string? ReferralReason, out DateTime RequestedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PatientId = this.PatientId;
        DestinationPartnerId = this.DestinationPartnerId;
        ReferringProviderId = this.ReferringProviderId;
        ReferralReason = this.ReferralReason;
        RequestedAtUtc = this.RequestedAtUtc;
    }
}
