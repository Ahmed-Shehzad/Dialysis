using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers.InMemory;
using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Engine;
using Dialysis.Simulation.Engine.Generation;
using Dialysis.Simulation.Engine.Scenarios;
using Dialysis.Simulation.Persistence;
using Dialysis.Simulation.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.Simulation.Tests;

public sealed class SimulationPersistenceTests
{
    private static readonly DateTimeOffset _clock = new(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Engine_Persists_Session_Events_And_Links_Async()
    {
        await using var ctx = NewContext();
        var sessions = new EfSimulationSessionRepository(ctx);
        var writeStore = new EfSimulationWriteStore(ctx);
        var queryStore = new EfSimulationQueryStore(ctx);
        var tp = new FixedTimeProvider(_clock);

        var session = SeedSession(ctx, sessions, "outpatient-lab");
        await ctx.SaveChangesAsync(CancellationToken.None);

        var scenario = new OutpatientLabScenario(new InMemoryEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver(), new StubPdfRenderer());
        var engine = new SimulationEngine(
            new ScenarioRegistry([scenario]), sessions, writeStore, ctx, new RecordingOutbox(), tp, NullLogger<SimulationEngine>.Instance);

        await engine.RunAsync(session.Id, CancellationToken.None);

        var loaded = await queryStore.GetSessionAsync(session.Id, CancellationToken.None);
        loaded.ShouldNotBeNull();
        loaded!.Status.ShouldBe(SimulationSessionStatus.Completed);
        loaded.PatientJourney.RealPatientId.ShouldNotBeNull();

        var events = await queryStore.ListEventsAsync(session.Id, 100, CancellationToken.None);
        events.Count.ShouldBe(scenario.Steps.Count);

        ctx.SessionRecordLinks.Count(l => l.SimulationSessionId == session.Id).ShouldBe(6);
    }

    [Fact]
    public async Task Failed_Session_Persists_Failure_State_Async()
    {
        await using var ctx = NewContext();
        var sessions = new EfSimulationSessionRepository(ctx);
        var writeStore = new EfSimulationWriteStore(ctx);
        var queryStore = new EfSimulationQueryStore(ctx);
        var tp = new FixedTimeProvider(_clock);

        var session = SeedSession(ctx, sessions, "outpatient-lab");
        await ctx.SaveChangesAsync(CancellationToken.None);

        var scenario = new OutpatientLabScenario(new ThrowingEhrDriver(), new InMemoryHisDriver(), new InMemoryLabDriver(), new InMemoryHieDriver(), new StubPdfRenderer());
        var engine = new SimulationEngine(
            new ScenarioRegistry([scenario]), sessions, writeStore, ctx, new RecordingOutbox(), tp, NullLogger<SimulationEngine>.Instance);

        await engine.RunAsync(session.Id, CancellationToken.None);

        var loaded = await queryStore.GetSessionAsync(session.Id, CancellationToken.None);
        loaded!.Status.ShouldBe(SimulationSessionStatus.Failed);
        loaded.WorkflowState.ShouldBe(WorkflowState.Failed);

        var events = await queryStore.ListEventsAsync(session.Id, 100, CancellationToken.None);
        events.ShouldContain(e => e.EventType == "WorkflowFailed");
    }

    private static SimulationSession SeedSession(SimulationDbContext ctx, EfSimulationSessionRepository sessions, string scenarioId)
    {
        var journey = new BogusJourneyGenerator().Generate(scenarioId, "tenant-x", 11);
        var session = SimulationSession.Start(
            scenarioId, "tenant-x", "org-x", 11, Guid.CreateVersion7(),
            journey.MedicalRecordNumber, journey.FamilyName, journey.GivenName, journey.DateOfBirth, journey.SexAtBirthCode,
            _clock.UtcDateTime);
        sessions.Add(session);
        _ = ctx;
        return session;
    }

    private static SimulationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SimulationDbContext>()
            .UseInMemoryDatabase($"sim-{Guid.NewGuid():N}")
            .Options;
        var persistence = Options.Create(new TransponderPersistenceOptions { Schema = "transponder" });
        return new SimulationDbContext(options, persistence);
    }
}
