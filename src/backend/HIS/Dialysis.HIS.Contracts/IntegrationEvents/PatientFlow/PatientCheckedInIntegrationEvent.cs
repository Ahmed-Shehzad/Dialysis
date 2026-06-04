using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a receptionist checks in a previously-expected patient. EHR may mirror the
/// patient locally on first sight; PDMS may pre-warm chairside fixtures; HIE may stage a
/// future Encounter export. Carries name + MRN so consumers don't have to fan out for a
/// follow-up lookup before they can render or audit.
/// </summary>
public sealed record PatientCheckedInIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a receptionist checks in a previously-expected patient. EHR may mirror the
    /// patient locally on first sight; PDMS may pre-warm chairside fixtures; HIE may stage a
    /// future Encounter export. Carries name + MRN so consumers don't have to fan out for a
    /// follow-up lookup before they can render or audit.
    /// </summary>
    public PatientCheckedInIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid EntryId,
        Guid PatientId,
        string PatientName,
        string Mrn,
        DateTime CheckedInAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.EntryId = EntryId;
        this.PatientId = PatientId;
        this.PatientName = PatientName;
        this.Mrn = Mrn;
        this.CheckedInAtUtc = CheckedInAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid EntryId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; }
    public string Mrn { get; init; }
    public DateTime CheckedInAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid EntryId, out Guid PatientId, out string PatientName, out string Mrn, out DateTime CheckedInAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        EntryId = this.EntryId;
        PatientId = this.PatientId;
        PatientName = this.PatientName;
        Mrn = this.Mrn;
        CheckedInAtUtc = this.CheckedInAtUtc;
    }
}
