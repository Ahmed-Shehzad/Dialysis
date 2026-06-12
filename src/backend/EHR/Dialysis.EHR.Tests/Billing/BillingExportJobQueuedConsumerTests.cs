using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIS.Contracts.IntegrationEvents.Billing;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Tests for the HIS → EHR billing-export bridge. The consumer submits the payer's assembled claims as
/// the EDI 837 batch (stamping control numbers + moving them to Submitted) and reports the outcome back
/// to HIS so the export job leaves Queued. A window with no ready claims is a successful no-op batch.
/// </summary>
public sealed class BillingExportJobQueuedConsumerTests
{
    [Fact]
    public async Task Submits_Matching_Payer_Claims_And_Reports_Batch_Async()
    {
        var aetnaA = AssembledClaim("AETNA", 100m);
        var aetnaB = AssembledClaim("AETNA", 150m);
        var medicare = AssembledClaim("MEDICARE", 999m);
        var claims = new StubClaimRepo(aetnaA, aetnaB, medicare);
        var outbox = new CapturingOutbox();
        var unitOfWork = new StubUnitOfWork();
        var consumer = Consumer(claims, outbox, unitOfWork);

        var jobId = Guid.CreateVersion7();
        await consumer.HandleAsync(Context(NewEvent(jobId, "AETNA")));

        // The two AETNA claims shipped; MEDICARE was left untouched.
        aetnaA.Status.ShouldBe(ClaimStatus.Submitted);
        aetnaA.ExternalControlNumber.ShouldNotBeNullOrWhiteSpace();
        aetnaB.Status.ShouldBe(ClaimStatus.Submitted);
        medicare.Status.ShouldBe(ClaimStatus.Assembled);
        unitOfWork.Saved.ShouldBeTrue();

        // The batch outcome rides the transactional outbox, not the bus.
        var completed = outbox.EventsOf<BillingExportJobCompletedIntegrationEvent>().ShouldHaveSingleItem();
        completed.JobId.ShouldBe(jobId);
        completed.PayerCode.ShouldBe("AETNA");
        completed.ClaimCount.ShouldBe(2);
        completed.BilledTotal.ShouldBe(250m);
        completed.CurrencyCode.ShouldBe("USD");
    }

    [Fact]
    public async Task Empty_Window_Completes_As_Zero_Claim_Batch_Async()
    {
        var claims = new StubClaimRepo(AssembledClaim("MEDICARE", 100m));
        var outbox = new CapturingOutbox();
        var consumer = Consumer(claims, outbox, new StubUnitOfWork());

        await consumer.HandleAsync(Context(NewEvent(Guid.CreateVersion7(), "AETNA")));

        var completed = outbox.EventsOf<BillingExportJobCompletedIntegrationEvent>().ShouldHaveSingleItem();
        completed.ClaimCount.ShouldBe(0);
        completed.BilledTotal.ShouldBe(0m);
    }

    private static BillingExportJobQueuedConsumer Consumer(
        IClaimRepository claims, CapturingOutbox outbox, IUnitOfWork unitOfWork) =>
        new(claims, outbox, new CapturingBus(), TimeProvider.System, unitOfWork,
            NullLogger<BillingExportJobQueuedConsumer>.Instance);

    private static Claim AssembledClaim(string payerCode, decimal amount)
    {
        var patientId = Guid.CreateVersion7();
        var charge = Charge.Capture(Guid.CreateVersion7(), patientId, Guid.CreateVersion7(), "90935", ["N18.6"], new Money(amount, "USD"));
        return Claim.Assemble(Guid.CreateVersion7(), patientId, Guid.CreateVersion7(), payerCode, "837P", [charge]);
    }

    private static BillingExportJobQueuedIntegrationEvent NewEvent(Guid jobId, string payerCode) => new(
        EventId: Guid.CreateVersion7(),
        OccurredOn: DateTime.UtcNow,
        SchemaVersion: 1,
        JobId: jobId,
        PayerCode: payerCode,
        PeriodStart: new DateOnly(2026, 5, 1),
        PeriodEnd: new DateOnly(2026, 5, 31),
        Notes: null);

    private static ConsumeContext<BillingExportJobQueuedIntegrationEvent> Context(BillingExportJobQueuedIntegrationEvent message) =>
        new(message, CancellationToken.None, new CapturingBus());


    private sealed class CapturingOutbox : ITransponderOutbox
    {
        public List<TransponderOutboxEnvelope> Enqueued { get; } = new();
        public Task EnqueueAsync(TransponderOutboxEnvelope envelope, CancellationToken cancellationToken = default)
        {
            Enqueued.Add(envelope);
            return Task.CompletedTask;
        }

        public IEnumerable<T> EventsOf<T>() => Enqueued
            .Where(e => e.AssemblyQualifiedEventType.StartsWith(typeof(T).FullName!, StringComparison.Ordinal))
            .Select(e => System.Text.Json.JsonSerializer.Deserialize<T>(e.PayloadJson)!);
    }

    private sealed class CapturingBus : ITransponderBus
    {
        public List<object> Published { get; } = new();
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }

    private sealed class StubClaimRepo : IClaimRepository
    {
        private readonly List<Claim> _claims;
        public StubClaimRepo(params Claim[] claims) => _claims = [.. claims];
        public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Claim?>(_claims.FirstOrDefault(c => c.Id == id));
        public Task<Claim?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<Claim?>(_claims.FirstOrDefault(c => c.ExternalControlNumber == controlNumber));
        public Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Claim>>(
                [.. _claims.Where(c => status is null || c.Status == status.Value).Take(take)]);
        public void Add(Claim claim) => _claims.Add(claim);
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
