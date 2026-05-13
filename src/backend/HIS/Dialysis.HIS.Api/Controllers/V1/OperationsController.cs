using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Operations.Features.AssignStaffRole;
using Dialysis.HIS.Operations.Features.RecordInventoryMovement;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Generic MIS</em> — staff and inventory at the dialysis facility. Billing claims live in the EHR module.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/operations")]
public sealed class OperationsController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("staff/{staffMemberId:guid}/primary-role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignStaffPrimaryRole(
        Guid staffMemberId,
        [FromBody] AssignStaffRoleBody body,
        CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<AssignStaffPrimaryRoleCommand, Unit>(
                new AssignStaffPrimaryRoleCommand(staffMemberId, body.RoleCode),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("inventory/items/{inventoryItemId:guid}/movements")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordInventoryMovement(
        Guid inventoryItemId,
        [FromBody] InventoryMovementBody body,
        CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<RecordInventoryMovementCommand, Unit>(
                new RecordInventoryMovementCommand(inventoryItemId, body.DeltaQuantity),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record AssignStaffRoleBody(string RoleCode);

    public sealed record InventoryMovementBody(int DeltaQuantity);
}
