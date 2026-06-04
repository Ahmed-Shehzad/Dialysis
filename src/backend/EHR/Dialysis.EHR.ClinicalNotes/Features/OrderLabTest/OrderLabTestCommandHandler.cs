using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed class OrderLabTestCommandHandler : ICommandHandler<OrderLabTestCommand, Guid>
{
    private readonly ILabOrderRepository _labOrders;
    private readonly IUnitOfWork _unitOfWork;
    public OrderLabTestCommandHandler(ILabOrderRepository labOrders,
        IUnitOfWork unitOfWork)
    {
        _labOrders = labOrders;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(OrderLabTestCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var order = LabOrder.Order(
            id,
            request.PatientId,
            request.EncounterId,
            request.OrderingProviderId,
            request.LabFacilityCode,
            request.LoincPanelCodes,
            request.TransmissionFormat);
        _labOrders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
