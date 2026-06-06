using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.Simulation.Contracts;

namespace Dialysis.Simulation.Engine.Domain;

/// <summary>Thrown when a scenario step declares a workflow transition the state machine forbids.</summary>
public sealed class InvalidWorkflowTransitionException : DomainException
{
    /// <summary>Creates the exception for the forbidden <paramref name="from"/> → <paramref name="to"/> edge.</summary>
    public InvalidWorkflowTransitionException(WorkflowState from, WorkflowState to)
        : base($"Workflow cannot transition from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    /// <summary>The source state.</summary>
    public WorkflowState From { get; }

    /// <summary>The rejected target state.</summary>
    public WorkflowState To { get; }
}

/// <summary>
/// The patient-journey workflow state machine. A scenario declares an ordered subset of edges; this
/// table validates each one so a scenario can never drive an impossible transition (e.g. billing before
/// an encounter). <see cref="WorkflowState.Failed"/> is reachable from any non-terminal state, and a
/// non-terminal state may transition to itself (an action that does not advance the journey, such as
/// requesting a referral within an open encounter).
/// </summary>
public static class WorkflowStateMachine
{
    private static readonly IReadOnlyDictionary<WorkflowState, WorkflowState[]> _allowed =
        new Dictionary<WorkflowState, WorkflowState[]>
        {
            [WorkflowState.Created] = [WorkflowState.Registered],
            [WorkflowState.Registered] = [WorkflowState.AppointmentBooked, WorkflowState.EncounterStarted, WorkflowState.Admitted],
            [WorkflowState.AppointmentBooked] = [WorkflowState.EncounterStarted],
            [WorkflowState.EncounterStarted] = [WorkflowState.Admitted, WorkflowState.LabOrdered, WorkflowState.BillingReady, WorkflowState.DocumentsReady, WorkflowState.Discharged, WorkflowState.Completed],
            [WorkflowState.Admitted] = [WorkflowState.Icu, WorkflowState.EncounterStarted, WorkflowState.LabOrdered, WorkflowState.Discharged],
            [WorkflowState.Icu] = [WorkflowState.EncounterStarted, WorkflowState.LabOrdered, WorkflowState.Discharged],
            [WorkflowState.LabOrdered] = [WorkflowState.ResultAvailable],
            [WorkflowState.ResultAvailable] = [WorkflowState.BillingReady, WorkflowState.DocumentsReady],
            [WorkflowState.BillingReady] = [WorkflowState.DocumentsReady, WorkflowState.Discharged],
            [WorkflowState.DocumentsReady] = [WorkflowState.Discharged, WorkflowState.Completed],
            [WorkflowState.Discharged] = [WorkflowState.Completed],
            [WorkflowState.Completed] = [],
            [WorkflowState.Failed] = [],
        };

    /// <summary>True when the workflow may move from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static bool CanTransition(WorkflowState from, WorkflowState to)
    {
        if (from is WorkflowState.Completed or WorkflowState.Failed)
            return false;
        if (to == WorkflowState.Failed)
            return true;
        if (to == from)
            return true;
        return _allowed.TryGetValue(from, out var targets) && Array.IndexOf(targets, to) >= 0;
    }

    /// <summary>Throws <see cref="InvalidWorkflowTransitionException"/> unless the transition is allowed.</summary>
    public static void EnsureCanTransition(WorkflowState from, WorkflowState to)
    {
        if (!CanTransition(from, to))
            throw new InvalidWorkflowTransitionException(from, to);
    }
}
