using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.ReviewImagingAiFinding;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Consumers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

/// <summary>Domain + consumer + sign-off coverage for the advisory AI imaging finding.</summary>
public sealed class ImagingAiFindingTests
{
    private static ImagingOrder Order() =>
        ImagingOrder.Order(Guid.CreateVersion7(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "US", "VascularAccess", null);

    [Fact]
    public void RecordAiFinding_Sets_Pending_Review()
    {
        var order = Order();

        var recorded = order.RecordAiFinding("m0", "RID1", "http://radlex.org", "Patent access", 0.6, "Normal", "summary");

        recorded.ShouldBeTrue();
        order.AiFindingStatus.ShouldBe(AiFindingReviewStatus.PendingReview);
        order.AiFindingCode.ShouldBe("RID1");
        order.AiReviewedBy.ShouldBeNull();
    }

    [Fact]
    public void Review_Accept_Then_Record_Is_No_Op()
    {
        var order = Order();
        order.RecordAiFinding("m0", "RID1", null, "Patent access", 0.6, "Normal", null);
        order.ReviewAiFinding(accepted: true, reviewedBy: "dr-house", nowUtc: DateTime.UtcNow);

        order.AiFindingStatus.ShouldBe(AiFindingReviewStatus.Accepted);
        order.AiReviewedBy.ShouldBe("dr-house");

        // A re-delivered producer event must not overwrite the human decision.
        var recordedAgain = order.RecordAiFinding("m0", "RID2", null, "Different", 0.9, "Abnormal", null);
        recordedAgain.ShouldBeFalse();
        order.AiFindingStatus.ShouldBe(AiFindingReviewStatus.Accepted);
        order.AiFindingCode.ShouldBe("RID1");
    }

    [Fact]
    public void Review_Without_Pending_Finding_Throws()
    {
        var order = Order();
        Should.Throw<InvalidOperationException>(() => order.ReviewAiFinding(true, "dr-house", DateTime.UtcNow));
    }

    [Fact]
    public async Task Consumer_Attaches_Finding_By_Accession_Async()
    {
        var order = Order();
        var repo = new FakeRepo(order);
        var uow = new CountingUow();
        var consumer = new ImagingAiFindingProducedConsumer(repo, uow, NullLogger<ImagingAiFindingProducedConsumer>.Instance);

        await consumer.HandleAsync(Context(Event(order.AccessionNumber)));

        order.AiFindingStatus.ShouldBe(AiFindingReviewStatus.PendingReview);
        order.AiModelId.ShouldBe("sample-heuristic-v0");
        uow.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Consumer_Ignores_Unknown_Accession_Async()
    {
        var repo = new FakeRepo();
        var uow = new CountingUow();
        var consumer = new ImagingAiFindingProducedConsumer(repo, uow, NullLogger<ImagingAiFindingProducedConsumer>.Instance);

        await consumer.HandleAsync(Context(Event("IMG-NOPE")));

        uow.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Review_Handler_Records_Sign_Off_Async()
    {
        var order = Order();
        order.RecordAiFinding("m0", "RID1", null, "Patent access", 0.6, "Normal", null);
        var repo = new FakeRepo(order);
        var handler = new ReviewImagingAiFindingCommandHandler(repo, new CountingUow(), TimeProvider.System);

        await handler.HandleAsync(new ReviewImagingAiFindingCommand(order.Id, Accepted: false, ReviewedBy: "dr-house"), CancellationToken.None);

        order.AiFindingStatus.ShouldBe(AiFindingReviewStatus.Rejected);
        order.AiReviewedBy.ShouldBe("dr-house");
    }

    private static ImagingAiFindingProducedIntegrationEvent Event(string accession) =>
        new(Guid.NewGuid(), DateTime.UtcNow, 1, accession, "1.2.3", Guid.NewGuid(), "sample-heuristic-v0",
            "RID1", "http://radlex.org", "Patent access", 0.6, "Normal", "summary", RequiresHumanReview: true);

    private static ConsumeContext<ImagingAiFindingProducedIntegrationEvent> Context(ImagingAiFindingProducedIntegrationEvent ev) =>
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
