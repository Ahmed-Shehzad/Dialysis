using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Medication.Domain.ValueObjects;
using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed class PlaceMedicationOrderCommandHandler(
    IMedicationOrderRepository orders,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PlaceMedicationOrderCommand, Guid>
{
    public async Task<Guid> Handle(PlaceMedicationOrderCommand request, CancellationToken cancellationToken)
    {
        var order = MedicationOrder.Place(
            request.PatientId,
            new DrugCode(request.DrugCode),
            new Dosage(request.Dosage),
            DateTime.UtcNow);

        orders.Add(order);

        foreach (var @event in order.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        order.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.Id;
    }
}
