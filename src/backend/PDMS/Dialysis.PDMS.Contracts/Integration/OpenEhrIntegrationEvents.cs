using Dialysis.DomainDrivenDesign.IntegrationEvents;
using Dialysis.PDMS.Contracts.CodeSets;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Parallel openEHR-shaped projection of a haemodialysis session transition (start / complete / abort).
/// Conforms to <c>openEHR-EHR-COMPOSITION.haemodialysis_session.v1</c>.
/// </summary>
public sealed record HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid SessionId,
    Guid PatientId,
    HaemodialysisSessionPhase Phase,
    string ArchetypeId,
    string CompositionJson,
    DateTime PhaseAtUtc) : IIntegrationEvent;
