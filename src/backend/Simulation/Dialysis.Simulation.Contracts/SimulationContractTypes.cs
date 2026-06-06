namespace Dialysis.Simulation.Contracts;

/// <summary>
/// A simulation session projection — its scenario, multi-tenant lineage, deterministic seed, status,
/// and the current workflow state.
/// </summary>
public sealed record SimulationSessionDto(
    Guid SimulationSessionId,
    string ScenarioId,
    string TenantId,
    string OrganizationId,
    string CorrelationId,
    string TraceId,
    long Seed,
    SimulationSessionStatus Status,
    WorkflowState WorkflowState,
    string? FailureReason,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc);

/// <summary>
/// One row of a session's event store. Every business step the engine drives emits exactly one of
/// these, carrying the full Scenario/Workflow/PatientJourney/Correlation/Trace lineage so no event
/// can exist outside a session.
/// </summary>
public sealed record SimulationEventDto(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    string AggregateType,
    string TenantId,
    string OrganizationId,
    Guid SimulationSessionId,
    string ScenarioId,
    Guid WorkflowId,
    Guid PatientJourneyId,
    string CorrelationId,
    string TraceId,
    DateTime OccurredAtUtc,
    string? Payload,
    int Version);

/// <summary>One audit-trail entry for a major action the engine took within a session.</summary>
public sealed record SimulationAuditDto(
    Guid AuditId,
    Guid SimulationSessionId,
    string Action,
    string ActorContext,
    string? Detail,
    DateTime OccurredAtUtc);

/// <summary>Catalog summary of a registered scenario and its ordered step names.</summary>
public sealed record ScenarioSummaryDto(
    string ScenarioId,
    string Name,
    string Description,
    IReadOnlyList<string> Steps);

/// <summary>
/// Request body to start a session. <paramref name="Seed"/> + <paramref name="ScenarioId"/> +
/// <paramref name="TenantId"/> make the generated patient journey deterministic.
/// </summary>
public sealed record StartSimulationRequest(
    string ScenarioId,
    string TenantId,
    string OrganizationId,
    long Seed);
