using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.RecordInventoryMovement;

public sealed record RecordInventoryMovementCommand : ICommand, IPermissionedCommand
{
    public RecordInventoryMovementCommand(Guid InventoryItemId, int DeltaQuantity)
    {
        this.InventoryItemId = InventoryItemId;
        this.DeltaQuantity = DeltaQuantity;
    }
    public string RequiredPermission => HisPermissions.InventoryMove;
    public Guid InventoryItemId { get; init; }
    public int DeltaQuantity { get; init; }
    public void Deconstruct(out Guid InventoryItemId, out int DeltaQuantity)
    {
        InventoryItemId = this.InventoryItemId;
        DeltaQuantity = this.DeltaQuantity;
    }
}
