using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Tests for auto charge capture on encounter close. The consumer captures one Charge per procedure
/// CPT (priced via the fee schedule), is idempotent on (EncounterId, CptCode), is a no-op when the
/// flag is off, and skips (rather than throws on) a CPT with no fee-schedule row.
/// </summary>
public sealed class EncounterClosedChargeConsumerTests
{
    [Fact]
    public async Task Enabled_Captures_One_Charge_Per_Cpt_With_Encounter_Diagnoses_Async()
    {
        var charges = new StubChargeRepo();
        var uow = new StubUnitOfWork();
        var consumer = Consumer(charges, new StubFeeSchedule(), uow, enabled: true);

        await consumer.HandleAsync(Context(NewEvent(cpts: ["90935", "36415"], diagnoses: ["N18.6"])));

        charges.Added.Count.ShouldBe(2);
        charges.Added.Select(c => c.CptCode).ShouldBe(["90935", "36415"], ignoreOrder: true);
        charges.Added.ShouldAllBe(c => c.DiagnosisPointerIcd10Codes.Contains("N18.6"));
        uow.Saved.ShouldBeTrue();
    }

    [Fact]
    public async Task Redelivery_Is_Idempotent_Async()
    {
        var encounterId = Guid.NewGuid();
        var charges = new StubChargeRepo();
        var consumer = Consumer(charges, new StubFeeSchedule(), new StubUnitOfWork(), enabled: true);

        var evt = NewEvent(encounterId: encounterId, cpts: ["90935"], diagnoses: ["N18.6"]);
        await consumer.HandleAsync(Context(evt));
        await consumer.HandleAsync(Context(evt));

        charges.Added.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Disabled_Flag_Captures_Nothing_Async()
    {
        var charges = new StubChargeRepo();
        var uow = new StubUnitOfWork();
        var consumer = Consumer(charges, new StubFeeSchedule(), uow, enabled: false);

        await consumer.HandleAsync(Context(NewEvent(cpts: ["90935"], diagnoses: ["N18.6"])));

        charges.Added.ShouldBeEmpty();
        uow.Saved.ShouldBeFalse();
    }

    [Fact]
    public async Task Missing_Fee_Row_Skips_That_Cpt_And_Does_Not_Throw_Async()
    {
        var charges = new StubChargeRepo();
        // "99999" has no fee row → that line is skipped; the priced CPT is still captured.
        var consumer = Consumer(charges, new StubFeeSchedule(unpriced: "99999"), new StubUnitOfWork(), enabled: true);

        await consumer.HandleAsync(Context(NewEvent(cpts: ["99999", "90935"], diagnoses: ["N18.6"])));

        charges.Added.ShouldHaveSingleItem().CptCode.ShouldBe("90935");
    }

    [Fact]
    public async Task No_Diagnoses_Captures_Nothing_Async()
    {
        var charges = new StubChargeRepo();
        var consumer = Consumer(charges, new StubFeeSchedule(), new StubUnitOfWork(), enabled: true);

        await consumer.HandleAsync(Context(NewEvent(cpts: ["90935"], diagnoses: [])));

        charges.Added.ShouldBeEmpty();
    }

    private static EncounterClosedChargeConsumer Consumer(
        IChargeRepository charges, ICptFeeSchedule fees, IUnitOfWork uow, bool enabled) =>
        new(charges, new StubIdempotency(), fees, uow,
            Options.Create(new EncounterChargeAutomationOptions { Enabled = enabled }),
            NullLogger<EncounterClosedChargeConsumer>.Instance);

    private static EncounterClosedIntegrationEvent NewEvent(
        Guid? encounterId = null,
        IReadOnlyList<string>? cpts = null,
        IReadOnlyList<string>? diagnoses = null) => new(
        EventId: Guid.CreateVersion7(),
        OccurredOn: DateTime.UtcNow,
        SchemaVersion: 1,
        EncounterId: encounterId ?? Guid.NewGuid(),
        PatientId: Guid.NewGuid(),
        ProviderId: Guid.NewGuid(),
        ClosedAtUtc: DateTime.UtcNow,
        DiagnosisIcd10Codes: diagnoses ?? ["N18.6"],
        ProcedureCptCodes: cpts ?? ["90935"]);

    private static ConsumeContext<EncounterClosedIntegrationEvent> Context(EncounterClosedIntegrationEvent message) =>
        new(message, CancellationToken.None, new NoopBus());

    private sealed class StubFeeSchedule : ICptFeeSchedule
    {
        private readonly HashSet<string> _unpriced;
        public StubFeeSchedule(params string[] unpriced) => _unpriced = new(unpriced, StringComparer.OrdinalIgnoreCase);
        public Task<Money> LookupAsync(string cptCode, CancellationToken cancellationToken)
        {
            if (_unpriced.Contains(cptCode))
                throw new InvalidOperationException($"No fee-schedule entry for {cptCode}.");
            return Task.FromResult(new Money(100m, "USD"));
        }
    }

    private sealed class StubChargeRepo : IChargeRepository
    {
        public List<Charge> Added { get; } = new();
        public void Add(Charge charge) => Added.Add(charge);
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Charge?>(Added.FirstOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>([.. Added.Where(c => c.PatientId == patientId && c.Status == ChargeStatus.Captured)]);
        public Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>([.. Added.Where(c => status is null || c.Status == status.Value).Take(take)]);
        public Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>([.. Added.Where(c => c.PatientId == patientId)]);
        public Task<IReadOnlyList<Charge>> ListAgedCapturedAsync(DateTime capturedBeforeUtc, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>([.. Added.Where(c => c.Status == ChargeStatus.Captured).Take(take)]);
    }

    private sealed class StubIdempotency : IChargeIdempotencyStore
    {
        private readonly Dictionary<(Guid, string), Guid> _seen = new();
        public Task<Guid?> FindChargeIdAsync(Guid sessionId, string cptCode, CancellationToken cancellationToken)
            => Task.FromResult(_seen.TryGetValue((sessionId, cptCode), out var id) ? (Guid?)id : null);
        public Task RegisterAsync(Guid sessionId, string cptCode, Guid chargeId, CancellationToken cancellationToken)
        {
            _seen[(sessionId, cptCode)] = chargeId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.FromResult(0);
        }
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }
}
