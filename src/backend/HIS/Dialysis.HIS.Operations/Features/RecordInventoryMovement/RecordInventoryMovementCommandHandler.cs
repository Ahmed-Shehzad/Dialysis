using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.RecordInventoryMovement;

public sealed class RecordInventoryMovementCommandHandler(IInventoryRepository inventory, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordInventoryMovementCommand>
{
    public async Task<Unit> Handle(RecordInventoryMovementCommand request, CancellationToken cancellationToken)
    {
        var item = await inventory.GetAsync(request.InventoryItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventory item not found.");

        var next = item.QuantityOnHand + request.DeltaQuantity;
        if (next < 0)
            throw new InvalidOperationException("Quantity cannot be negative.");

        item.QuantityOnHand = next;
        inventory.Update(item);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
