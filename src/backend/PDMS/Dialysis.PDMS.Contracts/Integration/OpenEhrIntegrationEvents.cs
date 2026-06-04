using Dialysis.DomainDrivenDesign.IntegrationEvents;
using Dialysis.PDMS.Contracts.CodeSets;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Parallel openEHR-shaped projection of a haemodialysis session transition (start / complete / abort).
/// Conforms to <c>openEHR-EHR-COMPOSITION.haemodialysis_session.v1</c>.
/// </summary>
public sealed record HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Parallel openEHR-shaped projection of a haemodialysis session transition (start / complete / abort).
    /// Conforms to <c>openEHR-EHR-COMPOSITION.haemodialysis_session.v1</c>.
    /// </summary>
    public HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        HaemodialysisSessionPhase Phase,
        string ArchetypeId,
        string CompositionJson,
        DateTime PhaseAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.Phase = Phase;
        this.ArchetypeId = ArchetypeId;
        this.CompositionJson = CompositionJson;
        this.PhaseAtUtc = PhaseAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public HaemodialysisSessionPhase Phase { get; init; }
    public string ArchetypeId { get; init; }
    public string CompositionJson { get; init; }
    public DateTime PhaseAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out HaemodialysisSessionPhase Phase, out string ArchetypeId, out string CompositionJson, out DateTime PhaseAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        Phase = this.Phase;
        ArchetypeId = this.ArchetypeId;
        CompositionJson = this.CompositionJson;
        PhaseAtUtc = this.PhaseAtUtc;
    }
}
