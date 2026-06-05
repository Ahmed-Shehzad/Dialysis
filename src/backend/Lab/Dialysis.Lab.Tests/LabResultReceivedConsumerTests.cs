using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Lab.Orders.Consumers;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.Lab.Tests;

/// <summary>
/// Coverage for the result-receiving consumer that closes the lab loop: it matches an inbound
/// result on the placer order number, records the observations, and results the order — and is a
/// no-op when the placer order number is unknown.
/// </summary>
public sealed class LabResultReceivedConsumerTests
{
    private static readonly DateTime _resultedAt = new(2026, 6, 5, 11, 30, 0, DateTimeKind.Utc);
    private static readonly IDialysisCodeValidator _codeValidator = new DialysisCodeValidator(new DialysisTerminologyCatalog());

    [Fact]
    public async Task Records_Results_And_Resolves_Matching_Order_Async()
    {
        var order = LabOrder.Place(
            patientId: Guid.NewGuid(),
            tests: [new LabTestItem("2160-0", "Creatinine")],
            priority: LabOrderPriority.Stat,
            specimen: "Serum",
            placedBy: "dr-house",
            nowUtc: DateTime.UtcNow);

        var repository = new FakeRepository(order);
        var unitOfWork = new CountingUnitOfWork();
        var consumer = new LabResultReceivedConsumer(repository, unitOfWork, _codeValidator, NullLogger<LabResultReceivedConsumer>.Instance);

        var ev = new LabResultReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PlacerOrderNumber: order.PlacerOrderNumber,
            FillerOrderNumber: "FILL-99",
            PatientId: order.PatientId,
            Status: LabOrderStatus.Resulted,
            Observations:
            [
                new LabObservationContract("2160-0", "Creatinine", "1.4", "mg/dL", "0.6-1.3", LabResultInterpretation.High),
            ],
            ResultedAtUtc: _resultedAt);

        await consumer.HandleAsync(Context(ev));

        order.Status.ShouldBe(LabOrderStatus.Resulted);
        order.FillerOrderNumber.ShouldBe("FILL-99");
        order.ResultedAtUtc.ShouldBe(_resultedAt);
        order.Results.Count.ShouldBe(1);
        order.Results.First().Interpretation.ShouldBe(LabResultInterpretation.High);
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Ignores_Result_For_Unknown_Placer_Order_Number_Async()
    {
        var repository = new FakeRepository();
        var unitOfWork = new CountingUnitOfWork();
        var consumer = new LabResultReceivedConsumer(repository, unitOfWork, _codeValidator, NullLogger<LabResultReceivedConsumer>.Instance);

        var ev = new LabResultReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PlacerOrderNumber: "LAB-DOESNOTEXIST",
            FillerOrderNumber: null,
            PatientId: Guid.NewGuid(),
            Status: LabOrderStatus.Resulted,
            Observations: [new LabObservationContract("2160-0", "Creatinine", "1.0", "mg/dL", null, LabResultInterpretation.Normal)],
            ResultedAtUtc: _resultedAt);

        await consumer.HandleAsync(Context(ev));

        unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Normalizes_A_Local_Lab_Code_To_Loinc_When_Recording_Async()
    {
        var order = LabOrder.Place(
            patientId: Guid.NewGuid(),
            tests: [new LabTestItem("2160-0", "Creatinine")],
            priority: LabOrderPriority.Routine,
            specimen: "Serum",
            placedBy: "dr-house",
            nowUtc: DateTime.UtcNow);

        var repository = new FakeRepository(order);
        var unitOfWork = new CountingUnitOfWork();
        var consumer = new LabResultReceivedConsumer(repository, unitOfWork, _codeValidator, NullLogger<LabResultReceivedConsumer>.Instance);

        var ev = new LabResultReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PlacerOrderNumber: order.PlacerOrderNumber,
            FillerOrderNumber: "FILL-100",
            PatientId: order.PatientId,
            Status: LabOrderStatus.Resulted,
            // Inbound feed sent the local mnemonic "CR" in the code slot; the consumer should map it to LOINC.
            Observations: [new LabObservationContract("CR", "Creatinine (local)", "1.2", "mg/dL", "0.6-1.3", LabResultInterpretation.Normal)],
            ResultedAtUtc: _resultedAt);

        await consumer.HandleAsync(Context(ev));

        order.Results.Count.ShouldBe(1);
        order.Results.First().LoincCode.ShouldBe("2160-0");
    }

    private static ConsumeContext<LabResultReceivedIntegrationEvent> Context(LabResultReceivedIntegrationEvent ev) =>
        new(ev, CancellationToken.None, new NoopBus());

    private sealed class FakeRepository : ILabOrderRepository
    {
        private readonly List<LabOrder> _orders;
        public FakeRepository(params LabOrder[] seed) => _orders = [.. seed];

        public void Add(LabOrder order) => _orders.Add(order);

        public Task<LabOrder?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

        public Task<LabOrder?> FindByPlacerOrderNumberAsync(string placerOrderNumber, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.FirstOrDefault(o =>
                string.Equals(o.PlacerOrderNumber, placerOrderNumber, StringComparison.Ordinal)));

        public Task<IReadOnlyList<LabOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LabOrder>>([.. _orders.Where(o => o.PatientId == patientId).Take(take)]);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
