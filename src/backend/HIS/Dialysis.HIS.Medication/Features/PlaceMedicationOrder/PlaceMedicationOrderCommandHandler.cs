using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed class PlaceMedicationOrderCommandHandler(
    IMedicationOrderRepository orders,
    IMedicationOrderSafetyPolicy safety,
    IUnitOfWork unitOfWork,
    ITransponderOutbox outbox)
    : ICommandHandler<PlaceMedicationOrderCommand, Guid>
{
    public async Task<Guid> Handle(PlaceMedicationOrderCommand request, CancellationToken cancellationToken)
    {
        safety.EnsureCanPlace(request.PatientId, request.MedicationCode);
        var id = Guid.CreateVersion7();
        var order = MedicationOrder.Place(id, request.PatientId, request.MedicationCode, DateTime.UtcNow, actorId: null);
        orders.Add(order);
        foreach (var evt in order.IntegrationEvents.ToArray())
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(evt), cancellationToken).ConfigureAwait(false);
        order.ClearIntegrationEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
