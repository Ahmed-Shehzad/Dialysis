using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication.Features.RecordMedicationAdministration;

public sealed class RecordMedicationAdministrationCommandHandler(IMedicationOrderRepository orders, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordMedicationAdministrationCommand>
{
    public async Task<Unit> Handle(RecordMedicationAdministrationCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetAsync(request.OrderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Order not found.");

        order.RecordAdministration(DateTime.UtcNow, actorId: null);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
