using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.Simulation.Contracts;

namespace Dialysis.Simulation.Engine.Domain;

/// <summary>
/// Aggregate root for one execution of a scenario. Owns the multi-tenant lineage (tenant/org/
/// correlation/trace), the deterministic seed, the lifecycle <see cref="SimulationSessionStatus"/>,
/// the current <see cref="WorkflowState"/>, and the <see cref="PatientJourney"/>. The engine advances
/// the workflow one validated transition at a time; the session is the consistency boundary that
/// guarantees no generated record is an orphan.
/// </summary>
public sealed class SimulationSession : AggregateRoot<Guid>
{
    private SimulationSession()
    {
    }

    private SimulationSession(Guid id) : base(id)
    {
    }

    /// <summary>The scenario being executed.</summary>
    public string ScenarioId { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Owning organization.</summary>
    public string OrganizationId { get; private set; } = null!;

    /// <summary>Run correlation id (threaded onto every driver call).</summary>
    public string CorrelationId { get; private set; } = null!;

    /// <summary>Run trace id.</summary>
    public string TraceId { get; private set; } = null!;

    /// <summary>Deterministic generation seed.</summary>
    public long Seed { get; private set; }

    /// <summary>Lifecycle status.</summary>
    public SimulationSessionStatus Status { get; private set; }

    /// <summary>Current workflow state.</summary>
    public WorkflowState WorkflowState { get; private set; }

    /// <summary>Failure reason once the workflow has failed; otherwise <c>null</c>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>When the session was started (UTC).</summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>When the session completed or failed (UTC); <c>null</c> while running.</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>The generated patient journey.</summary>
    public PatientJourney PatientJourney { get; private set; } = null!;

    /// <summary>
    /// Starts a session: generates fresh run lineage, captures the seed, and builds the patient journey.
    /// </summary>
    public static SimulationSession Start(
        string scenarioId,
        string tenantId,
        string organizationId,
        long seed,
        Guid patientJourneyId,
        string medicalRecordNumber,
        string familyName,
        string givenName,
        DateOnly dateOfBirth,
        string sexAtBirthCode,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new DomainException("A simulation session requires a scenario.");
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new DomainException("A simulation session requires a tenant.");

        var id = Guid.CreateVersion7();
        var session = new SimulationSession(id)
        {
            ScenarioId = scenarioId.Trim(),
            TenantId = tenantId.Trim(),
            OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? tenantId.Trim() : organizationId.Trim(),
            CorrelationId = Guid.CreateVersion7().ToString("N"),
            TraceId = Guid.CreateVersion7().ToString("N"),
            Seed = seed,
            Status = SimulationSessionStatus.Created,
            WorkflowState = WorkflowState.Created,
            StartedAtUtc = nowUtc,
        };
        session.PatientJourney = PatientJourney.Create(
            patientJourneyId, id, medicalRecordNumber, familyName, givenName, dateOfBirth, sexAtBirthCode);
        return session;
    }

    /// <summary>Marks the session running as the engine begins walking the scenario.</summary>
    public void MarkRunning()
    {
        if (Status == SimulationSessionStatus.Created)
            Status = SimulationSessionStatus.Running;
    }

    /// <summary>Advances the workflow to <paramref name="next"/>, validating the transition.</summary>
    public void AdvanceTo(WorkflowState next)
    {
        WorkflowStateMachine.EnsureCanTransition(WorkflowState, next);
        WorkflowState = next;
    }

    /// <summary>Completes the session once the workflow has reached <see cref="WorkflowState.Completed"/>.</summary>
    public void Complete(DateTime nowUtc)
    {
        if (WorkflowState != WorkflowState.Completed)
            throw new DomainException($"Cannot complete a session whose workflow is {WorkflowState}.");
        Status = SimulationSessionStatus.Completed;
        CompletedAtUtc = nowUtc;
    }

    /// <summary>Fails the session: drives the workflow to FAILED and records the reason.</summary>
    public void Fail(string reason, DateTime nowUtc)
    {
        WorkflowState = WorkflowState.Failed;
        Status = SimulationSessionStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown failure." : reason.Trim();
        CompletedAtUtc = nowUtc;
    }
}
