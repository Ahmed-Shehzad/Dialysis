using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers;
using Dialysis.Simulation.Engine.Domain;

namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>
/// Mutable scratchpad threaded through a scenario's steps. Each step reads the ids produced by earlier
/// steps (patient, encounter, lab order, …) and records the ids it produces, so the next step can use
/// them — keeping the generated graph fully connected.
/// </summary>
public sealed class SimulationContext
{
    /// <summary>Creates a context for a session.</summary>
    public SimulationContext(SimulationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        Driver = new DriverContext(session.TenantId, session.CorrelationId, session.TraceId);
        ProviderId = DeterministicGuid.From($"{session.CorrelationId}:provider");
    }

    /// <summary>The session being executed.</summary>
    public SimulationSession Session { get; }

    /// <summary>Lineage carried onto every driver call.</summary>
    public DriverContext Driver { get; }

    /// <summary>A deterministic provider id for the run.</summary>
    public Guid ProviderId { get; }

    /// <summary>The real EHR patient id (set by the registration step).</summary>
    public Guid? RealPatientId { get; set; }

    /// <summary>The booked appointment id, if any.</summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>The started encounter id, if any.</summary>
    public Guid? EncounterId { get; set; }

    /// <summary>The admission id, if any.</summary>
    public Guid? AdmissionId { get; set; }

    /// <summary>The placed lab order id, if any.</summary>
    public Guid? LabOrderId { get; set; }

    /// <summary>The placer order number for the placed lab order, if any.</summary>
    public string? PlacerOrderNumber { get; set; }

    /// <summary>Charges captured when the encounter was closed.</summary>
    public IReadOnlyList<CapturedCharge> Charges { get; set; } = [];
}

/// <summary>A record link a step asks the engine to persist (session → real-module record).</summary>
public sealed record RecordLinkSpec(string ModuleSlug, string RecordType, string RealRecordId);

/// <summary>
/// The outcome of a step: the event the engine should record (with payload) plus any record links.
/// </summary>
public sealed record StepResult(
    string EventType,
    string AggregateType,
    Guid AggregateId,
    string? Payload,
    IReadOnlyList<RecordLinkSpec> Links)
{
    /// <summary>Convenience factory for a step that produced a single real-module record.</summary>
    public static StepResult ForRecord(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string moduleSlug,
        string recordType,
        string? payload = null) =>
        new(eventType, aggregateType, aggregateId, payload,
            [new RecordLinkSpec(moduleSlug, recordType, aggregateId.ToString("N"))]);

    /// <summary>Convenience factory for a step that produced no external record (a marker event).</summary>
    public static StepResult Marker(string eventType, Guid aggregateId, string? payload = null) =>
        new(eventType, "Simulation", aggregateId, payload, []);
}

/// <summary>One ordered step of a scenario.</summary>
public sealed record ScenarioStep(
    string Name,
    WorkflowState ToState,
    int MaxRetries,
    Func<SimulationContext, CancellationToken, Task<StepResult>> ExecuteAsync);

/// <summary>A scenario: an ordered list of steps that drive a patient journey across modules.</summary>
public interface IScenario
{
    /// <summary>Stable scenario id (used to start a session).</summary>
    string Id { get; }

    /// <summary>Human-readable name.</summary>
    string Name { get; }

    /// <summary>What the scenario exercises.</summary>
    string Description { get; }

    /// <summary>The ordered steps.</summary>
    IReadOnlyList<ScenarioStep> Steps { get; }
}
