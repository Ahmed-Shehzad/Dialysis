using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Engine.Domain;

namespace Dialysis.Simulation.Engine.Features;

/// <summary>Projects engine aggregates/records to the public contract DTOs.</summary>
internal static class SimulationMappings
{
    public static SimulationSessionDto ToDto(this SimulationSession s) =>
        new(
            s.Id,
            s.ScenarioId,
            s.TenantId,
            s.OrganizationId,
            s.CorrelationId,
            s.TraceId,
            s.Seed,
            s.Status,
            s.WorkflowState,
            s.FailureReason,
            s.StartedAtUtc,
            s.CompletedAtUtc);

    public static SimulationEventDto ToDto(this SimulationEventRecord e) =>
        new(
            e.Id,
            e.EventType,
            e.AggregateId,
            e.AggregateType,
            e.TenantId,
            e.OrganizationId,
            e.SimulationSessionId,
            e.ScenarioId,
            e.WorkflowId,
            e.PatientJourneyId,
            e.CorrelationId,
            e.TraceId,
            e.OccurredAtUtc,
            e.Payload,
            e.Version);

    public static SimulationAuditDto ToDto(this SimulationAuditEntry a) =>
        new(a.Id, a.SimulationSessionId, a.Action, a.ActorContext, a.Detail, a.OccurredAtUtc);
}
