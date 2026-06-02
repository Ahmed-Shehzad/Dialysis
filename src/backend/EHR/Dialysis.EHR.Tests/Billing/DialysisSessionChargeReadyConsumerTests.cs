using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Idempotency-focused tests for the PDMS → EHR charge bridge. The consumer must capture
/// a <see cref="Charge"/> on first delivery and exit silently on re-delivery — RabbitMQ
/// can deliver the same event twice on a reconnect and the charge ledger must not double.
/// </summary>
public sealed class DialysisSessionChargeReadyConsumerTests
{
    [Fact]
    public async Task First_Delivery_Captures_A_Charge_With_The_Resolved_Fee_Async()
    {
        var charges = new StubChargeRepo();
        var idempotency = new StubIdempotency();
        var feeSchedule = new StubFeeSchedule(new Money(250.00m, "USD"));
        var unitOfWork = new StubUnitOfWork();
        var consumer = new DialysisSessionChargeReadyConsumer(
            charges, idempotency, feeSchedule, unitOfWork, NullLogger<DialysisSessionChargeReadyConsumer>.Instance);

        await consumer.HandleAsync(Context(NewEvent(modality: "HD", cpt: "90935")));

        charges.Added.Count.ShouldBe(1);
        charges.Added[0].CptCode.ShouldBe("90935");
        charges.Added[0].BilledAmount.Amount.ShouldBe(250.00m);
        unitOfWork.Saved.ShouldBeTrue();
    }

    [Fact]
    public async Task Redelivery_Does_Not_Create_A_Duplicate_Charge_Async()
    {
        var sessionId = Guid.NewGuid();
        var charges = new StubChargeRepo();
        var idempotency = new StubIdempotency();
        var fee = new StubFeeSchedule(new Money(250.00m, "USD"));
        var consumer = new DialysisSessionChargeReadyConsumer(
            charges, idempotency, fee, new StubUnitOfWork(), NullLogger<DialysisSessionChargeReadyConsumer>.Instance);

        var first = NewEvent(sessionId: sessionId, modality: "HD", cpt: "90935");

        await consumer.HandleAsync(Context(first));
        await consumer.HandleAsync(Context(first));

        charges.Added.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Diagnosis_Pointer_Defaults_To_N18_6_For_Haemo_And_Peritoneal_Async()
    {
        var charges = new StubChargeRepo();
        var consumer = new DialysisSessionChargeReadyConsumer(
            charges, new StubIdempotency(), new StubFeeSchedule(new Money(100m, "USD")),
            new StubUnitOfWork(), NullLogger<DialysisSessionChargeReadyConsumer>.Instance);

        foreach (var modality in new[] { "HD", "PD", "Haemodialysis", "Peritoneal" })
        {
            await consumer.HandleAsync(Context(NewEvent(modality: modality, cpt: "90935")));
        }

        charges.Added.Count.ShouldBe(4);
        charges.Added.ShouldAllBe(c => c.DiagnosisPointerIcd10Codes.Contains("N18.6"));
    }

    private static DialysisSessionChargeReadyIntegrationEvent NewEvent(
        Guid? sessionId = null,
        string modality = "HD",
        string cpt = "90935") => new(
        EventId: Guid.CreateVersion7(),
        OccurredOn: DateTime.UtcNow,
        SchemaVersion: 1,
        SessionId: sessionId ?? Guid.NewGuid(),
        PatientId: Guid.NewGuid(),
        Modality: modality,
        DurationMinutes: 240,
        CompletedAtUtc: DateTime.UtcNow,
        CptCode: cpt);

    private static ConsumeContext<DialysisSessionChargeReadyIntegrationEvent> Context(DialysisSessionChargeReadyIntegrationEvent message) =>
        new(message, CancellationToken.None, NullBus.Instance);

    private sealed class NullBus : ITransponderBus
    {
        public static NullBus Instance { get; } = new();
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }

    private sealed class StubChargeRepo : IChargeRepository
    {
        public List<Charge> Added { get; } = new();
        public void Add(Charge charge) => Added.Add(charge);
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Charge?>(Added.FirstOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>(Added.Where(c => c.PatientId == patientId && c.Status == ChargeStatus.Captured).ToArray());
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

    private sealed class StubFeeSchedule(Money amount) : ICptFeeSchedule
    {
        public Task<Money> LookupAsync(string cptCode, CancellationToken cancellationToken) => Task.FromResult(amount);
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
}
