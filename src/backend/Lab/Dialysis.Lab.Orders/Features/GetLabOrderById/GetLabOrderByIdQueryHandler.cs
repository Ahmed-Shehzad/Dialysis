using Dialysis.CQRS.Queries;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;

namespace Dialysis.Lab.Orders.Features.GetLabOrderById;

public sealed class GetLabOrderByIdQueryHandler : IQueryHandler<GetLabOrderByIdQuery, LabOrderDto?>
{
    private readonly ILabOrderRepository _orders;
    public GetLabOrderByIdQueryHandler(ILabOrderRepository orders) => _orders = orders;

    public async Task<LabOrderDto?> HandleAsync(GetLabOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return order is null ? null : ToDto(order);
    }

    internal static LabOrderDto ToDto(LabOrder order) =>
        new(
            order.Id,
            order.PatientId,
            order.PlacerOrderNumber,
            order.FillerOrderNumber,
            order.Priority,
            order.Status,
            order.Specimen,
            order.PlacedBy,
            order.PlacedAtUtc,
            order.ResultedAtUtc,
            [.. order.Tests.Select(t => new LabTestRequestContract(t.LoincCode, t.Display))],
            [.. order.Results.Select(r => new LabObservationContract(
                r.LoincCode, r.Display, r.Value, r.Unit, r.ReferenceRange, r.Interpretation))]);
}
