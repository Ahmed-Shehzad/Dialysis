using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed class OrderLabTestCommandHandler(
    ILabOrderRepository labOrders,
    IUnitOfWork unitOfWork)
    : ICommandHandler<OrderLabTestCommand, Guid>
{
    public async Task<Guid> Handle(OrderLabTestCommand request, CancellationToken cancellationToken)
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
        labOrders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
