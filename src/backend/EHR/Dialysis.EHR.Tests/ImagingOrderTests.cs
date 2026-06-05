using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests;

/// <summary>Domain + handler coverage for the imaging-ordering slice.</summary>
public sealed class ImagingOrderTests
{
    private static ImagingOrder NewOrder() =>
        ImagingOrder.Order(Guid.CreateVersion7(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "us", "Kidney", "AVF surveillance");

    [Fact]
    public void Order_Generates_Accession_Number_And_Raises_Placed_Event()
    {
        var order = NewOrder();

        order.Status.ShouldBe(ImagingOrderStatus.Ordered);
        order.ModalityCode.ShouldBe("US"); // normalised upper
        order.AccessionNumber.ShouldStartWith("IMG-");
        order.StudyInstanceUid.ShouldBeNull();

        var placed = order.IntegrationEvents.OfType<ImagingOrderPlacedIntegrationEvent>().ShouldHaveSingleItem();
        placed.AccessionNumber.ShouldBe(order.AccessionNumber);
        placed.ModalityCode.ShouldBe("US");
        placed.BodySiteCode.ShouldBe("Kidney");
    }

    [Fact]
    public void LinkStudy_Records_Uid_And_Completes()
    {
        var order = NewOrder();

        order.LinkStudy("1.2.840.113619.2.55.3.123");

        order.StudyInstanceUid.ShouldBe("1.2.840.113619.2.55.3.123");
        order.Status.ShouldBe(ImagingOrderStatus.Completed);
    }

    [Fact]
    public void Cancel_Then_Link_Study_Throws()
    {
        var order = NewOrder();
        order.Cancel("duplicate");

        order.Status.ShouldBe(ImagingOrderStatus.Cancelled);
        Should.Throw<InvalidOperationException>(() => order.LinkStudy("1.2.3"));
    }

    [Fact]
    public void Order_Requires_Modality_And_Body_Site()
    {
        Should.Throw<ArgumentException>(() =>
            ImagingOrder.Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", "Kidney", null));
        Should.Throw<ArgumentException>(() =>
            ImagingOrder.Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "US", " ", null));
    }

    [Fact]
    public async Task Handler_Persists_The_Order_Async()
    {
        var repo = new FakeRepo();
        var id = await new OrderImagingStudyCommandHandler(repo, new NoopUow())
            .HandleAsync(
                new OrderImagingStudyCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CT", "Chest", null),
                CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        repo.Added.ShouldHaveSingleItem();
        repo.Added[0].ModalityCode.ShouldBe("CT");
    }

    private sealed class FakeRepo : IImagingOrderRepository
    {
        public List<ImagingOrder> Added { get; } = [];
        public void Add(ImagingOrder imagingOrder) => Added.Add(imagingOrder);
        public Task<ImagingOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Added.FirstOrDefault(o => o.Id == id));
        public Task<ImagingOrder?> GetByAccessionNumberAsync(string accessionNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(Added.FirstOrDefault(o => o.AccessionNumber == accessionNumber));
        public Task<IReadOnlyList<ImagingOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImagingOrder>>([.. Added.Where(o => o.PatientId == patientId).Take(take)]);
    }

    private sealed class NoopUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
