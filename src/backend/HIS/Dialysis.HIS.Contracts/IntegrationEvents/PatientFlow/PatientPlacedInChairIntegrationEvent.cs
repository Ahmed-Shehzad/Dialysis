using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a waiting patient is moved into a chair (treatment about to start).
/// PDMS is the primary consumer — pairs the chair identifier with an incoming dialysis
/// session so the chairside view can resolve patient context before vitals arrive.
/// </summary>
public sealed record PatientPlacedInChairIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a waiting patient is moved into a chair (treatment about to start).
    /// PDMS is the primary consumer — pairs the chair identifier with an incoming dialysis
    /// session so the chairside view can resolve patient context before vitals arrive.
    /// </summary>
    public PatientPlacedInChairIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid EntryId,
        Guid PatientId,
        string Chair,
        DateTime PlacedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.EntryId = EntryId;
        this.PatientId = PatientId;
        this.Chair = Chair;
        this.PlacedAtUtc = PlacedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid EntryId { get; init; }
    public Guid PatientId { get; init; }
    public string Chair { get; init; }
    public DateTime PlacedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid EntryId, out Guid PatientId, out string Chair, out DateTime PlacedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        EntryId = this.EntryId;
        PatientId = this.PatientId;
        Chair = this.Chair;
        PlacedAtUtc = this.PlacedAtUtc;
    }
}
