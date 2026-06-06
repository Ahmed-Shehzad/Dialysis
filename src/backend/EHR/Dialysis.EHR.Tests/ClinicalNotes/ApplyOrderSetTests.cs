using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.Features.OrderSets;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalNotes;

public sealed class ApplyOrderSetTests
{
    private static OrderSet IntakeSet()
    {
        var set = OrderSet.Create(Guid.NewGuid(), "Dialysis intake", null, DateTime.UtcNow);
        set.AddLabLine(Guid.NewGuid(), "LAB-1", ["24323-8"]);
        set.AddMedicationLine(Guid.NewGuid(), "29046", "Lisinopril 10mg", "1 tab", "daily", 30, 3, "PHARM-1");
        set.AddImagingLine(Guid.NewGuid(), "US", "VascularAccess", "AVF check");
        return set;
    }

    [Fact]
    public async Task Apply_Fans_Out_One_Command_Per_Line_Async()
    {
        var set = IntakeSet();
        var gateway = new FakeGateway();
        var handler = new ApplyOrderSetCommandHandler(new SingleOrderSetRepo(set), gateway);

        var result = await handler.HandleAsync(
            new ApplyOrderSetCommand(set.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Orders.Count.ShouldBe(3);
        result.Orders.Select(o => o.Kind).ShouldBe(["Lab", "Medication", "Imaging"]);
        gateway.Dispatched.OfType<OrderLabTestCommand>().ShouldHaveSingleItem();
        gateway.Dispatched.OfType<OrderPrescriptionCommand>().ShouldHaveSingleItem();
        gateway.Dispatched.OfType<OrderImagingStudyCommand>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Apply_Propagates_A_Blocking_Advisory_Async()
    {
        var set = IntakeSet();
        var handler = new ApplyOrderSetCommandHandler(new SingleOrderSetRepo(set), new FakeGateway(blockLab: true));

        await Should.ThrowAsync<ClinicalSafetyBlockedException>(() => handler.HandleAsync(
            new ApplyOrderSetCommand(set.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class SingleOrderSetRepo : IOrderSetRepository
    {
        private readonly OrderSet _set;
        public SingleOrderSetRepo(OrderSet set) => _set = set;
        public Task<OrderSet?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrderSet?>(_set.Id == id ? _set : null);
        public Task<IReadOnlyList<OrderSet>> ListActiveAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrderSet>>([_set]);
        public void Add(OrderSet orderSet) { }
    }

    private sealed class FakeGateway : ICqrsGateway
    {
        private readonly bool _blockLab;
        public FakeGateway(bool blockLab = false) => _blockLab = blockLab;
        public List<object> Dispatched { get; } = [];

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse>
        {
            Dispatched.Add(command!);
            object result = command switch
            {
                OrderLabTestCommand when _blockLab => throw new ClinicalSafetyBlockedException(
                    [new SafetyAdvisory(AdvisoryCategory.DuplicateLabOrder, AdvisorySeverity.Blocking, "24323-8", "24323-8", "24323-8", Guid.NewGuid(), "LabOrder")]),
                OrderLabTestCommand => new OrderPlacementResult(Guid.NewGuid(), []),
                OrderPrescriptionCommand => new OrderPlacementResult(Guid.NewGuid(), []),
                OrderImagingStudyCommand => Guid.NewGuid(),
                _ => throw new NotSupportedException(),
            };
            return Task.FromResult((TResponse)result);
        }

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : Dialysis.CQRS.Queries.IQuery<TResponse> => throw new NotSupportedException();
    }
}
