using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.RecordInventoryMovement;

public sealed record RecordInventoryMovementCommand(Guid InventoryItemId, int DeltaQuantity)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.InventoryMove;
}
