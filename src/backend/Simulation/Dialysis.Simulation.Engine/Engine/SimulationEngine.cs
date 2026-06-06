using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.IntegrationEvents;
using Dialysis.Simulation.Contracts.Messaging;
using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Ports;
using Dialysis.Simulation.Engine.Scenarios;
using Microsoft.Extensions.Logging;

namespace Dialysis.Simulation.Engine.Engine;

/// <summary>
/// Walks a scenario's steps one validated workflow transition at a time. Every step either drives a
/// module (via a driver) or publishes an integration event; on success the engine records an
/// event-store row, an audit entry, and any record links in the same unit of work as the workflow
/// advance — so progress is durable and nothing is left orphaned. On terminal step failure the
/// workflow moves to FAILED and a <see cref="WorkflowFailedIntegrationEvent"/> is enqueued.
/// </summary>
public sealed class SimulationEngine : ISimulationEngine
{
    private const string ActorContext = "simulation-engine";

    private readonly IScenarioRegistry _scenarios;
    private readonly ISimulationSessionRepository _sessions;
    private readonly ISimulationWriteStore _writeStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransponderOutbox _outbox;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SimulationEngine> _logger;

    /// <summary>Creates the engine.</summary>
    public SimulationEngine(
        IScenarioRegistry scenarios,
        ISimulationSessionRepository sessions,
        ISimulationWriteStore writeStore,
        IUnitOfWork unitOfWork,
        ITransponderOutbox outbox,
        TimeProvider timeProvider,
        ILogger<SimulationEngine> logger)
    {
        _scenarios = scenarios;
        _sessions = sessions;
        _writeStore = writeStore;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessions.FindAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Simulation session {sessionId} was not found.");

        if (session.Status is SimulationSessionStatus.Completed or SimulationSessionStatus.Failed)
            return;

        var scenario = _scenarios.Find(session.ScenarioId);
        if (scenario is null)
        {
            await FailAsync(session, "resolve-scenario", $"Unknown scenario '{session.ScenarioId}'.", cancellationToken).ConfigureAwait(false);
            return;
        }

        session.MarkRunning();
        _writeStore.AppendAudit(NewAudit(session, "ScenarioStarted", scenario.Name));
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var context = new SimulationContext(session);

        foreach (var step in scenario.Steps)
        {
            if (!WorkflowStateMachine.CanTransition(session.WorkflowState, step.ToState))
            {
                await FailAsync(session, step.Name, $"Illegal transition {session.WorkflowState} → {step.ToState}.", cancellationToken).ConfigureAwait(false);
                return;
            }

            var (result, error) = await ExecuteWithRetryAsync(step, context, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                await FailAsync(session, step.Name, error?.Message ?? "Unknown step failure.", cancellationToken).ConfigureAwait(false);
                return;
            }

            session.AdvanceTo(step.ToState);
            RecordSuccess(session, step, result);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (session.WorkflowState == WorkflowState.Completed)
        {
            session.Complete(_timeProvider.GetUtcNow().UtcDateTime);
            _writeStore.AppendAudit(NewAudit(session, "SimulationCompleted", "All scenario steps completed."));
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<(StepResult? Result, Exception? Error)> ExecuteWithRetryAsync(
        ScenarioStep step, SimulationContext context, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt <= step.MaxRetries; attempt++)
        {
            try
            {
                var result = await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                return (result, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Simulation step {Step} failed on attempt {Attempt}.", step.Name, attempt + 1);
            }
        }

        return (null, lastError);
    }

    private void RecordSuccess(SimulationSession session, ScenarioStep step, StepResult result)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        _writeStore.AppendEvent(NewEvent(session, result.EventType, result.AggregateId, result.AggregateType, result.Payload, now));
        _writeStore.AppendAudit(new SimulationAuditEntry(
            Guid.CreateVersion7(), session.Id, $"StepCompleted:{step.Name}", ActorContext, result.Payload, now));
        foreach (var link in result.Links)
        {
            _writeStore.AppendLink(new SessionRecordLink(
                Guid.CreateVersion7(), session.Id, link.ModuleSlug, link.RecordType, link.RealRecordId, now));
        }
    }

    private async Task FailAsync(SimulationSession session, string step, string reason, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        session.Fail(reason, now);
        _writeStore.AppendEvent(NewEvent(session, "WorkflowFailed", session.Id, "Workflow", $"step={step}", now));
        _writeStore.AppendAudit(new SimulationAuditEntry(
            Guid.CreateVersion7(), session.Id, "WorkflowFailed", ActorContext, $"{step}: {reason}", now));
        await _outbox.EnqueueAsync(
            SimulationTransponderOutboxEnvelope.From(new WorkflowFailedIntegrationEvent(
                Guid.CreateVersion7(), now, 1, session.Id, session.ScenarioId, session.TenantId, step, reason, now)),
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Simulation session {SessionId} failed at step {Step}: {Reason}", session.Id, step, reason);
    }

    private static SimulationEventRecord NewEvent(
        SimulationSession session, string eventType, Guid aggregateId, string aggregateType, string? payload, DateTime nowUtc) =>
        new(
            Guid.CreateVersion7(),
            eventType,
            aggregateId,
            aggregateType,
            session.TenantId,
            session.OrganizationId,
            session.Id,
            session.ScenarioId,
            session.Id,
            session.PatientJourney.Id,
            session.CorrelationId,
            session.TraceId,
            nowUtc,
            payload,
            1);

    private SimulationAuditEntry NewAudit(SimulationSession session, string action, string? detail) =>
        new(Guid.CreateVersion7(), session.Id, action, ActorContext, detail, _timeProvider.GetUtcNow().UtcDateTime);
}
