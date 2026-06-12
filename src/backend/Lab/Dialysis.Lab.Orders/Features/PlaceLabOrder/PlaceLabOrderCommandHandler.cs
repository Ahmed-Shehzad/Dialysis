using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;

namespace Dialysis.Lab.Orders.Features.PlaceLabOrder;

public sealed class PlaceLabOrderCommandHandler : ICommandHandler<PlaceLabOrderCommand, Guid>
{
    private readonly ILabOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    public PlaceLabOrderCommandHandler(ILabOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(PlaceLabOrderCommand request, CancellationToken cancellationToken)
    {
        var tests = request.Tests.Select(t => new LabTestItem(t.LoincCode, t.Display)).ToList();
        var order = LabOrder.Place(request.PatientId, tests, request.Priority, request.Specimen, request.PlacedBy, DateTime.UtcNow);

        _orders.Add(order);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.Id;
    }
}
