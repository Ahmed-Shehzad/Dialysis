using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.RecordInventoryMovement;

public sealed class RecordInventoryMovementCommandHandler : ICommandHandler<RecordInventoryMovementCommand>
{
    private readonly IInventoryRepository _inventory;
    private readonly IUnitOfWork _unitOfWork;
    public RecordInventoryMovementCommandHandler(IInventoryRepository inventory, IUnitOfWork unitOfWork)
    {
        _inventory = inventory;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(RecordInventoryMovementCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventory.GetAsync(request.InventoryItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventory item not found.");

        item.RecordMovement(request.DeltaQuantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
