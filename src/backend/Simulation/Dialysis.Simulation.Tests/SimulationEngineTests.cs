using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers.InMemory;
using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Engine;
using Dialysis.Simulation.Engine.Generation;
using Dialysis.Simulation.Engine.Scenarios;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.Simulation.Tests;

public sealed class SimulationEngineTests
{
    private static readonly DateTimeOffset _clock = new(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Outpatient_Scenario_Completes_And_Records_Lineage_Async()
    {
        var backplane = new InMemoryBackplane();
        var outbox = new RecordingOutbox();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new OutpatientLabScenario(new InMemoryEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        var engine = NewEngine(scenario, backplane, outbox, tp);
        await engine.RunAsync(session.Id, CancellationToken.None);

        session.Status.ShouldBe(SimulationSessionStatus.Completed);
        session.WorkflowState.ShouldBe(WorkflowState.Completed);
        backplane.Events.Count.ShouldBe(scenario.Steps.Count);
        backplane.Events.ShouldContain(e => e.EventType == "PatientRegistered");
        backplane.Events.ShouldContain(e => e.EventType == "LabResultPublished");
        outbox.Enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Inpatient_Scenario_Completes_Async()
    {
        var backplane = new InMemoryBackplane();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new InpatientSurgeryScenario(new InMemoryEhrDriver(), new InMemoryHisDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, new RecordingOutbox(), tp).RunAsync(session.Id, CancellationToken.None);

        session.Status.ShouldBe(SimulationSessionStatus.Completed);
        session.WorkflowState.ShouldBe(WorkflowState.Completed);
        backplane.Events.ShouldContain(e => e.EventType == "PatientDischarged");
    }

    [Fact]
    public async Task Referral_Scenario_Completes_Async()
    {
        var backplane = new InMemoryBackplane();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new ReferralExchangeScenario(new InMemoryEhrDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, new RecordingOutbox(), tp).RunAsync(session.Id, CancellationToken.None);

        session.Status.ShouldBe(SimulationSessionStatus.Completed);
        backplane.Events.ShouldContain(e => e.EventType == "ReferralCreated");
    }

    [Fact]
    public async Task Failing_Step_Fails_The_Session_And_Enqueues_Event_Async()
    {
        var backplane = new InMemoryBackplane();
        var outbox = new RecordingOutbox();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new OutpatientLabScenario(new ThrowingEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, outbox, tp).RunAsync(session.Id, CancellationToken.None);

        session.Status.ShouldBe(SimulationSessionStatus.Failed);
        session.WorkflowState.ShouldBe(WorkflowState.Failed);
        session.FailureReason!.ShouldContain("driver-boom");
        backplane.Events.ShouldContain(e => e.EventType == "WorkflowFailed");
        outbox.Enqueued.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Retry_Recovers_From_A_Transient_Failure_Async()
    {
        var backplane = new InMemoryBackplane();
        var tp = new FixedTimeProvider(_clock);
        var flaky = new FlakyEhrDriver();
        var scenario = new OutpatientLabScenario(flaky, new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, new RecordingOutbox(), tp).RunAsync(session.Id, CancellationToken.None);

        session.Status.ShouldBe(SimulationSessionStatus.Completed);
        flaky.RegisterCalls.ShouldBe(2);
    }

    [Fact]
    public async Task Every_Event_Carries_Full_Lineage_Async()
    {
        var backplane = new InMemoryBackplane();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new OutpatientLabScenario(new InMemoryEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, new RecordingOutbox(), tp).RunAsync(session.Id, CancellationToken.None);

        backplane.Events.ShouldNotBeEmpty();
        foreach (var e in backplane.Events)
        {
            e.SimulationSessionId.ShouldBe(session.Id);
            e.WorkflowId.ShouldBe(session.Id);
            e.PatientJourneyId.ShouldBe(session.PatientJourney.Id);
            e.ScenarioId.ShouldBe(scenario.Id);
            e.TenantId.ShouldNotBeNullOrWhiteSpace();
            e.CorrelationId.ShouldNotBeNullOrWhiteSpace();
            e.TraceId.ShouldNotBeNullOrWhiteSpace();
            e.AggregateId.ShouldNotBe(Guid.Empty);
            e.Version.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Record_Links_Are_Written_For_Driven_Records_Async()
    {
        var backplane = new InMemoryBackplane();
        var tp = new FixedTimeProvider(_clock);
        var scenario = new OutpatientLabScenario(new InMemoryEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver());
        var session = StartSession(scenario.Id, backplane, tp);

        await NewEngine(scenario, backplane, new RecordingOutbox(), tp).RunAsync(session.Id, CancellationToken.None);

        backplane.Links.Select(l => l.ModuleSlug).Distinct().ShouldBe(["ehr", "his", "lab", "hie"], ignoreOrder: true);
        backplane.Links.ShouldAllBe(l => l.SimulationSessionId == session.Id);
    }

    private static SimulationEngine NewEngine(IScenario scenario, InMemoryBackplane backplane, RecordingOutbox outbox, TimeProvider tp) =>
        new(new ScenarioRegistry([scenario]), backplane, backplane, backplane, outbox, tp, NullLogger<SimulationEngine>.Instance);

    private static SimulationSession StartSession(string scenarioId, InMemoryBackplane backplane, TimeProvider tp)
    {
        var journey = new BogusJourneyGenerator().Generate(scenarioId, "tenant-a", 7);
        var session = SimulationSession.Start(
            scenarioId, "tenant-a", "org-1", 7, Guid.CreateVersion7(),
            journey.MedicalRecordNumber, journey.FamilyName, journey.GivenName, journey.DateOfBirth, journey.SexAtBirthCode,
            tp.GetUtcNow().UtcDateTime);
        backplane.Add(session);
        return session;
    }
}
