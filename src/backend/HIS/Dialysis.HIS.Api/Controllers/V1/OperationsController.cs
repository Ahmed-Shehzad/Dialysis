using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Operations.Features.AssignStaffRole;
using Dialysis.HIS.Operations.Features.GetBillingExportJobById;
using Dialysis.HIS.Operations.Features.RecordInventoryMovement;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>
/// RA: <em>Generic MIS</em> — staff, inventory, and billing-export queue at the dialysis facility.
/// Billing claims execute in the EHR module; HIS owns the queue/export-job trigger that fires
/// <c>BillingExportJobQueuedIntegrationEvent</c> for EHR to consume.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/operations")]
public sealed class OperationsController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("staff/{staffMemberId:guid}/primary-role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignStaffPrimaryRoleAsync(
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
    public async Task<IActionResult> RecordInventoryMovementAsync(
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

    /// <summary>RA Fig. 6 — Generic MIS → Finance/Reimbursement. Queues a payer-billing export for EHR to execute.</summary>
    [HttpPost("billing/export-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<SubmitBillingExportJobResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitBillingExportJobAsync(
        [FromBody] SubmitBillingExportJobCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<SubmitBillingExportJobCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/operations/billing/export-jobs/{id}",
            new SubmitBillingExportJobResponse(id),
            LinkCapabilitiesIndex());
    }

    [HttpGet("billing/export-jobs/{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<BillingExportJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBillingExportJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await gateway
            .SendQueryAsync<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>(
                new GetBillingExportJobByIdQuery(id),
                cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : OkResource(dto, LinkCapabilitiesIndex());
    }

    public sealed record AssignStaffRoleBody(string RoleCode);

    public sealed record InventoryMovementBody(int DeltaQuantity);

    public sealed record SubmitBillingExportJobResponse(Guid Id);
}
