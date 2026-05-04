using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Operations.Features.AssignStaffRole;
using Dialysis.HIS.Operations.Features.CreateBillingExportJob;
using Dialysis.HIS.Operations.Features.GetBillingExportJobById;
using Dialysis.HIS.Operations.Features.RecordInventoryMovement;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Generic MIS</em> — staff, inventory, billing export (Tummers et al., 2021).</summary>
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

    [HttpPost("billing/export-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<CreateBillingExportJobResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBillingExportJob([FromBody] CreateBillingExportJobCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<CreateBillingExportJobCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new CreateBillingExportJobResponse(id), LinkCapabilitiesIndex());
    }

    [HttpGet("billing/export-jobs/{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<BillingExportJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBillingExportJob(Guid id, CancellationToken cancellationToken)
    {
        var dto = await gateway
            .SendQueryAsync<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>(new GetBillingExportJobByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : OkResource(dto, LinkCapabilitiesIndex());
    }

    public sealed record AssignStaffRoleBody(string RoleCode);

    public sealed record InventoryMovementBody(int DeltaQuantity);

    public sealed record CreateBillingExportJobResponse(Guid Id);
}
