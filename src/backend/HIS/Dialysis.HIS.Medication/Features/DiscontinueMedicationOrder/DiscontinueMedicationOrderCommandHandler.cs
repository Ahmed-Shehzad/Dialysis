using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication.Features.DiscontinueMedicationOrder;

public sealed class DiscontinueMedicationOrderCommandHandler(
    IMedicationOrderRepository orders,
    IUnitOfWork unitOfWork,
    ITransponderOutbox outbox)
    : ICommandHandler<DiscontinueMedicationOrderCommand, Unit>
{
    public async Task<Unit> Handle(DiscontinueMedicationOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetAsync(request.OrderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Medication order not found.");

        order.Discontinue(DateTime.UtcNow, actorId: null);
        foreach (var evt in order.IntegrationEvents.ToArray())
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(evt), cancellationToken).ConfigureAwait(false);
        order.ClearIntegrationEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
