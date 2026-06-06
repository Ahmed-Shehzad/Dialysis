using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Simulation.Contracts.IntegrationEvents;

/// <summary>
/// Raised when a simulation scenario step exhausts its retries and the workflow transitions to FAILED.
/// Published over Transponder so observers (dashboards, downstream test harnesses) can react to a
/// stalled simulation without polling the session.
/// </summary>
public sealed record WorkflowFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Raised when a simulation scenario step fails terminally.</summary>
    public WorkflowFailedIntegrationEvent(
        Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SimulationSessionId,
        string ScenarioId,
        string TenantId,
        string FailedStep,
        string Reason,
        DateTime FailedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SimulationSessionId = SimulationSessionId;
        this.ScenarioId = ScenarioId;
        this.TenantId = TenantId;
        this.FailedStep = FailedStep;
        this.Reason = Reason;
        this.FailedAtUtc = FailedAtUtc;
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; }

    /// <inheritdoc />
    public int SchemaVersion { get; init; }

    /// <summary>The failed session.</summary>
    public Guid SimulationSessionId { get; init; }

    /// <summary>The scenario that was running.</summary>
    public string ScenarioId { get; init; }

    /// <summary>The owning tenant.</summary>
    public string TenantId { get; init; }

    /// <summary>The step that failed.</summary>
    public string FailedStep { get; init; }

    /// <summary>Human-readable failure reason.</summary>
    public string Reason { get; init; }

    /// <summary>When the failure was recorded (UTC).</summary>
    public DateTime FailedAtUtc { get; init; }
}
