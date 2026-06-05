using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;

public sealed class OrderImagingStudyCommandHandler : ICommandHandler<OrderImagingStudyCommand, Guid>
{
    private readonly IImagingOrderRepository _imagingOrders;
    private readonly IUnitOfWork _unitOfWork;
    public OrderImagingStudyCommandHandler(IImagingOrderRepository imagingOrders, IUnitOfWork unitOfWork)
    {
        _imagingOrders = imagingOrders;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(OrderImagingStudyCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var order = ImagingOrder.Order(
            id,
            request.PatientId,
            request.EncounterId,
            request.OrderingProviderId,
            request.ModalityCode,
            request.BodySiteCode,
            request.ReasonText);
        _imagingOrders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
