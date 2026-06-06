namespace Dialysis.Simulation.Engine.Domain;

/// <summary>
/// One immutable row of a session's event store. Carries the complete lineage so the no-orphan-events
/// invariant holds: an event cannot exist without a session, scenario, workflow, and patient journey.
/// </summary>
public sealed class SimulationEventRecord
{
    private SimulationEventRecord()
    {
    }

    /// <summary>Creates an event-store row.</summary>
    public SimulationEventRecord(
        Guid id,
        string eventType,
        Guid aggregateId,
        string aggregateType,
        string tenantId,
        string organizationId,
        Guid simulationSessionId,
        string scenarioId,
        Guid workflowId,
        Guid patientJourneyId,
        string correlationId,
        string traceId,
        DateTime occurredAtUtc,
        string? payload,
        int version)
    {
        Id = id;
        EventType = eventType;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        TenantId = tenantId;
        OrganizationId = organizationId;
        SimulationSessionId = simulationSessionId;
        ScenarioId = scenarioId;
        WorkflowId = workflowId;
        PatientJourneyId = patientJourneyId;
        CorrelationId = correlationId;
        TraceId = traceId;
        OccurredAtUtc = occurredAtUtc;
        Payload = payload;
        Version = version;
    }

#pragma warning disable CS1591
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public Guid AggregateId { get; private set; }
    public string AggregateType { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string OrganizationId { get; private set; } = null!;
    public Guid SimulationSessionId { get; private set; }
    public string ScenarioId { get; private set; } = null!;
    public Guid WorkflowId { get; private set; }
    public Guid PatientJourneyId { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string TraceId { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }
    public string? Payload { get; private set; }
    public int Version { get; private set; }
#pragma warning restore CS1591
}

/// <summary>One audit-trail entry for a major action the engine took within a session.</summary>
public sealed class SimulationAuditEntry
{
    private SimulationAuditEntry()
    {
    }

    /// <summary>Creates an audit entry.</summary>
    public SimulationAuditEntry(
        Guid id,
        Guid simulationSessionId,
        string action,
        string actorContext,
        string? detail,
        DateTime occurredAtUtc)
    {
        Id = id;
        SimulationSessionId = simulationSessionId;
        Action = action;
        ActorContext = actorContext;
        Detail = detail;
        OccurredAtUtc = occurredAtUtc;
    }

#pragma warning disable CS1591
    public Guid Id { get; private set; }
    public Guid SimulationSessionId { get; private set; }
    public string Action { get; private set; } = null!;
    public string ActorContext { get; private set; } = null!;
    public string? Detail { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
#pragma warning restore CS1591
}

/// <summary>
/// The no-orphan ledger: maps a session to a real-module record it created (so the data driven into
/// EHR/HIS/Lab/HIE can always be traced back to its originating session).
/// </summary>
public sealed class SessionRecordLink
{
    private SessionRecordLink()
    {
    }

    /// <summary>Creates a record link.</summary>
    public SessionRecordLink(
        Guid id,
        Guid simulationSessionId,
        string moduleSlug,
        string recordType,
        string realRecordId,
        DateTime createdAtUtc)
    {
        Id = id;
        SimulationSessionId = simulationSessionId;
        ModuleSlug = moduleSlug;
        RecordType = recordType;
        RealRecordId = realRecordId;
        CreatedAtUtc = createdAtUtc;
    }

#pragma warning disable CS1591
    public Guid Id { get; private set; }
    public Guid SimulationSessionId { get; private set; }
    public string ModuleSlug { get; private set; } = null!;
    public string RecordType { get; private set; } = null!;
    public string RealRecordId { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
#pragma warning restore CS1591
}
