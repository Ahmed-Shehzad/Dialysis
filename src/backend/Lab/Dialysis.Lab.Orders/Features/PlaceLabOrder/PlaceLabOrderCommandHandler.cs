using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Contracts.Messaging;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;

namespace Dialysis.Lab.Orders.Features.PlaceLabOrder;

public sealed class PlaceLabOrderCommandHandler : ICommandHandler<PlaceLabOrderCommand, Guid>
{
    private readonly ILabOrderRepository _orders;
    private readonly ITransponderOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    public PlaceLabOrderCommandHandler(ILabOrderRepository orders, ITransponderOutbox outbox, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(PlaceLabOrderCommand request, CancellationToken cancellationToken)
    {
        var tests = request.Tests.Select(t => new LabTestItem(t.LoincCode, t.Display)).ToList();
        var order = LabOrder.Place(request.PatientId, tests, request.Priority, request.Specimen, request.PlacedBy, DateTime.UtcNow);

        _orders.Add(order);

        foreach (var @event in order.IntegrationEvents)
        {
            await _outbox.EnqueueAsync(LabTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        order.ClearIntegrationEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.Id;
    }
}
