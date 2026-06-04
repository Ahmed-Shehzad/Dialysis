using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when an unannounced arrival is added to today's queue. Distinct from
/// <see cref="PatientCheckedInIntegrationEvent"/> because there was no prior appointment
/// to expect — downstream modules typically need to create the patient/encounter
/// scaffolding fresh rather than mirror an existing one.
/// </summary>
public sealed record WalkInRegisteredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when an unannounced arrival is added to today's queue. Distinct from
    /// <see cref="PatientCheckedInIntegrationEvent"/> because there was no prior appointment
    /// to expect — downstream modules typically need to create the patient/encounter
    /// scaffolding fresh rather than mirror an existing one.
    /// </summary>
    public WalkInRegisteredIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid EntryId,
        Guid PatientId,
        string PatientName,
        string Mrn,
        bool EligibilityVerified,
        DateTime RegisteredAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.EntryId = EntryId;
        this.PatientId = PatientId;
        this.PatientName = PatientName;
        this.Mrn = Mrn;
        this.EligibilityVerified = EligibilityVerified;
        this.RegisteredAtUtc = RegisteredAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid EntryId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; }
    public string Mrn { get; init; }
    public bool EligibilityVerified { get; init; }
    public DateTime RegisteredAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid EntryId, out Guid PatientId, out string PatientName, out string Mrn, out bool EligibilityVerified, out DateTime RegisteredAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        EntryId = this.EntryId;
        PatientId = this.PatientId;
        PatientName = this.PatientName;
        Mrn = this.Mrn;
        EligibilityVerified = this.EligibilityVerified;
        RegisteredAtUtc = this.RegisteredAtUtc;
    }
}
