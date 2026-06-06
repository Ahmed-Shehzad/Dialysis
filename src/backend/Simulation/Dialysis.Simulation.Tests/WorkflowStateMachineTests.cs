using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Engine.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.Simulation.Tests;

public sealed class WorkflowStateMachineTests
{
    [Theory]
    [InlineData(WorkflowState.Created, WorkflowState.Registered)]
    [InlineData(WorkflowState.Registered, WorkflowState.AppointmentBooked)]
    [InlineData(WorkflowState.Registered, WorkflowState.Admitted)]
    [InlineData(WorkflowState.AppointmentBooked, WorkflowState.EncounterStarted)]
    [InlineData(WorkflowState.EncounterStarted, WorkflowState.LabOrdered)]
    [InlineData(WorkflowState.EncounterStarted, WorkflowState.BillingReady)]
    [InlineData(WorkflowState.Admitted, WorkflowState.Icu)]
    [InlineData(WorkflowState.Icu, WorkflowState.EncounterStarted)]
    [InlineData(WorkflowState.LabOrdered, WorkflowState.ResultAvailable)]
    [InlineData(WorkflowState.ResultAvailable, WorkflowState.BillingReady)]
    [InlineData(WorkflowState.BillingReady, WorkflowState.DocumentsReady)]
    [InlineData(WorkflowState.DocumentsReady, WorkflowState.Completed)]
    [InlineData(WorkflowState.Discharged, WorkflowState.Completed)]
    public void Allowed_Transitions_Are_Permitted(WorkflowState from, WorkflowState to) =>
        WorkflowStateMachine.CanTransition(from, to).ShouldBeTrue();

    [Theory]
    [InlineData(WorkflowState.Created, WorkflowState.Completed)]
    [InlineData(WorkflowState.Created, WorkflowState.BillingReady)]
    [InlineData(WorkflowState.Registered, WorkflowState.ResultAvailable)]
    [InlineData(WorkflowState.LabOrdered, WorkflowState.Completed)]
    [InlineData(WorkflowState.Completed, WorkflowState.Registered)]
    [InlineData(WorkflowState.Failed, WorkflowState.Registered)]
    public void Illegal_Transitions_Are_Rejected(WorkflowState from, WorkflowState to) =>
        WorkflowStateMachine.CanTransition(from, to).ShouldBeFalse();

    [Theory]
    [InlineData(WorkflowState.Created)]
    [InlineData(WorkflowState.Registered)]
    [InlineData(WorkflowState.EncounterStarted)]
    [InlineData(WorkflowState.Icu)]
    [InlineData(WorkflowState.BillingReady)]
    public void Failed_Is_Reachable_From_Every_Non_Terminal_State(WorkflowState from) =>
        WorkflowStateMachine.CanTransition(from, WorkflowState.Failed).ShouldBeTrue();

    [Fact]
    public void Self_Transition_Is_Allowed_For_A_Non_Terminal_State() =>
        WorkflowStateMachine.CanTransition(WorkflowState.EncounterStarted, WorkflowState.EncounterStarted).ShouldBeTrue();

    [Fact]
    public void Terminal_States_Permit_No_Transition()
    {
        WorkflowStateMachine.CanTransition(WorkflowState.Completed, WorkflowState.Failed).ShouldBeFalse();
        WorkflowStateMachine.CanTransition(WorkflowState.Failed, WorkflowState.Completed).ShouldBeFalse();
    }

    [Fact]
    public void Ensure_Can_Transition_Throws_On_An_Illegal_Edge() =>
        Should.Throw<InvalidWorkflowTransitionException>(
            () => WorkflowStateMachine.EnsureCanTransition(WorkflowState.Created, WorkflowState.Completed));
}
