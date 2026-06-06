using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Billing.ReadModels;
using Dialysis.EHR.Contracts.Integration;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Tests the lost-charge read model: EncounterClosed records a billable encounter, ChargeCaptured flips
/// its HasCharge flag, and the worklist query returns only uncharged aged encounters.
/// </summary>
public sealed class BillableEncounterProjectionTests
{
    [Fact]
    public async Task Encounter_Closed_Upserts_A_Billable_Encounter_With_No_Charge_Async()
    {
        var repo = new InMemoryBillableEncounters();
        var projector = new EncounterClosedBillableProjector(repo, new NoopUnitOfWork());
        var encounterId = Guid.NewGuid();

        await projector.HandleAsync(ClosedContext(encounterId, DateTime.UtcNow.AddDays(-3)));

        var row = repo.Rows[encounterId];
        row.HasCharge.ShouldBeFalse();
    }

    [Fact]
    public async Task Charge_Captured_Flips_Has_Charge_Async()
    {
        var repo = new InMemoryBillableEncounters();
        var encounterId = Guid.NewGuid();
        await new EncounterClosedBillableProjector(repo, new NoopUnitOfWork())
            .HandleAsync(ClosedContext(encounterId, DateTime.UtcNow.AddDays(-3)));

        await new ChargeCapturedBillableProjector(repo).HandleAsync(CapturedContext(encounterId));

        repo.Rows[encounterId].HasCharge.ShouldBeTrue();
    }

    [Fact]
    public async Task Charge_Captured_For_An_Unknown_Encounter_Is_A_Noop_Async()
    {
        var repo = new InMemoryBillableEncounters();
        await new ChargeCapturedBillableProjector(repo).HandleAsync(CapturedContext(Guid.NewGuid()));
        repo.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task List_Missing_Charges_Returns_Only_Uncharged_Aged_Encounters_Async()
    {
        var repo = new InMemoryBillableEncounters();
        var now = DateTime.UtcNow;

        var aged = Guid.NewGuid();
        var recent = Guid.NewGuid();
        var charged = Guid.NewGuid();
        await repo.UpsertAsync(aged, Guid.NewGuid(), Guid.NewGuid(), now.AddDays(-5));     // uncharged + old → included
        await repo.UpsertAsync(recent, Guid.NewGuid(), Guid.NewGuid(), now.AddHours(-1));  // too recent → excluded
        await repo.UpsertAsync(charged, Guid.NewGuid(), Guid.NewGuid(), now.AddDays(-5));
        await repo.MarkHasChargeAsync(charged);                                            // has a charge → excluded

        var missing = await repo.ListMissingChargesAsync(now.AddDays(-2), 100);

        missing.ShouldHaveSingleItem().EncounterId.ShouldBe(aged);
    }

    private static ConsumeContext<EncounterClosedIntegrationEvent> ClosedContext(Guid encounterId, DateTime closedAtUtc) =>
        new(new EncounterClosedIntegrationEvent(
            EventId: Guid.CreateVersion7(), OccurredOn: DateTime.UtcNow, SchemaVersion: 1,
            EncounterId: encounterId, PatientId: Guid.NewGuid(), ProviderId: Guid.NewGuid(),
            ClosedAtUtc: closedAtUtc, DiagnosisIcd10Codes: ["N18.6"], ProcedureCptCodes: ["90935"]),
            CancellationToken.None, new NoopBus());

    private static ConsumeContext<ChargeCapturedIntegrationEvent> CapturedContext(Guid encounterId) =>
        new(new ChargeCapturedIntegrationEvent(
            EventId: Guid.CreateVersion7(), OccurredOn: DateTime.UtcNow, SchemaVersion: 1,
            ChargeId: Guid.NewGuid(), PatientId: Guid.NewGuid(), EncounterId: encounterId,
            CptCode: "90935", DiagnosisPointerIcd10Codes: ["N18.6"], BilledAmount: 100m, CurrencyCode: "USD"),
            CancellationToken.None, new NoopBus());

    private sealed class InMemoryBillableEncounters : IBillableEncounterRepository
    {
        public Dictionary<Guid, BillableEncounter> Rows { get; } = new();

        public Task UpsertAsync(Guid encounterId, Guid patientId, Guid providerId, DateTime closedAtUtc, CancellationToken cancellationToken = default)
        {
            var hadCharge = Rows.TryGetValue(encounterId, out var existing) && existing.HasCharge;
            Rows[encounterId] = new BillableEncounter
            {
                EncounterId = encounterId,
                PatientId = patientId,
                ProviderId = providerId,
                ClosedAtUtc = closedAtUtc,
                HasCharge = hadCharge,
            };
            return Task.CompletedTask;
        }

        public Task MarkHasChargeAsync(Guid encounterId, CancellationToken cancellationToken = default)
        {
            if (Rows.TryGetValue(encounterId, out var e))
                e.HasCharge = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BillableEncounter>> ListMissingChargesAsync(DateTime closedBeforeUtc, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BillableEncounter>>(
                [.. Rows.Values.Where(e => !e.HasCharge && e.ClosedAtUtc < closedBeforeUtc).OrderBy(e => e.ClosedAtUtc).Take(take)]);
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }
}
