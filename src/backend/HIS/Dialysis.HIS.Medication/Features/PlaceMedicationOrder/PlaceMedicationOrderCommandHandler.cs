using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Medication.Domain.ValueObjects;
using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed class PlaceMedicationOrderCommandHandler : ICommandHandler<PlaceMedicationOrderCommand, Guid>
{
    private readonly IMedicationOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    public PlaceMedicationOrderCommandHandler(IMedicationOrderRepository orders,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(PlaceMedicationOrderCommand request, CancellationToken cancellationToken)
    {
        var order = MedicationOrder.Place(
            request.PatientId,
            new DrugCode(request.DrugCode),
            new Dosage(request.Dosage),
            DateTime.UtcNow);

        _orders.Add(order);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.Id;
    }
}
