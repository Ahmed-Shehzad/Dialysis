using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Consumers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

/// <summary>Coverage for the consumer that links a fulfilled imaging study back to its order.</summary>
public sealed class ImagingStudyLinkedConsumerTests
{
    [Fact]
    public async Task Links_Study_To_Matching_Order_By_Accession_Async()
    {
        var order = ImagingOrder.Order(Guid.CreateVersion7(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "US", "Kidney", null);
        var repo = new FakeRepo(order);
        var uow = new CountingUow();
        var consumer = new ImagingStudyLinkedConsumer(repo, uow, NullLogger<ImagingStudyLinkedConsumer>.Instance);

        await consumer.HandleAsync(Context(Event(order.AccessionNumber, "1.2.840.55")));

        order.StudyInstanceUid.ShouldBe("1.2.840.55");
        order.Status.ShouldBe(ImagingOrderStatus.Completed);
        uow.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Ignores_Unknown_Accession_Async()
    {
        var repo = new FakeRepo();
        var uow = new CountingUow();
        var consumer = new ImagingStudyLinkedConsumer(repo, uow, NullLogger<ImagingStudyLinkedConsumer>.Instance);

        await consumer.HandleAsync(Context(Event("IMG-NOPE", "1.2.3")));

        uow.SaveCount.ShouldBe(0);
    }

    private static ImagingStudyLinkedIntegrationEvent Event(string accession, string studyUid) =>
        new(Guid.NewGuid(), DateTime.UtcNow, 1, Guid.NewGuid(), Guid.NewGuid(), accession, studyUid, 1, 12);

    private static ConsumeContext<ImagingStudyLinkedIntegrationEvent> Context(ImagingStudyLinkedIntegrationEvent ev) =>
        new(ev, CancellationToken.None, new NoopBus());

    private sealed class FakeRepo : IImagingOrderRepository
    {
        private readonly List<ImagingOrder> _store;
        public FakeRepo(params ImagingOrder[] seed) => _store = [.. seed];
        public void Add(ImagingOrder imagingOrder) => _store.Add(imagingOrder);
        public Task<ImagingOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(o => o.Id == id));
        public Task<ImagingOrder?> GetByAccessionNumberAsync(string accessionNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(o => o.AccessionNumber == accessionNumber));
        public Task<IReadOnlyList<ImagingOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImagingOrder>>([.. _store.Take(take)]);
    }

    private sealed class CountingUow : IUnitOfWork
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
