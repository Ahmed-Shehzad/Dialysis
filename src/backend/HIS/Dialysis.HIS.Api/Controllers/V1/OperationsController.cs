using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Operations.Features.AssignStaffRole;
using Dialysis.HIS.Operations.Features.ExecuteBillingExportJob;
using Dialysis.HIS.Operations.Features.GetBillingExportJobById;
using Dialysis.HIS.Operations.Features.ListBillingExportJobs;
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
public sealed class OperationsController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>
    /// RA: <em>Generic MIS</em> — staff, inventory, and billing-export queue at the dialysis facility.
    /// Billing claims execute in the EHR module; HIS owns the queue/export-job trigger that fires
    /// <c>BillingExportJobQueuedIntegrationEvent</c> for EHR to consume.
    /// </summary>
    public OperationsController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("staff/{staffMemberId:guid}/primary-role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignStaffPrimaryRoleAsync(
        Guid staffMemberId,
        [FromBody] AssignStaffRoleBody body,
        CancellationToken cancellationToken)
    {
        await _gateway
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
        await _gateway
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
        var id = await _gateway
            .SendCommandAsync<SubmitBillingExportJobCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/operations/billing/export-jobs/{id}",
            new SubmitBillingExportJobResponse(id),
            LinkCapabilitiesIndex());
    }

    /// <summary>
    /// Operator action — (re-)dispatch a queued billing-export job to EHR for assembly. Re-fires the
    /// queued trigger so EHR's billing pipeline assembles the EDI 837 batch and reports the outcome
    /// back (advancing the job out of <c>Queued</c>). Only jobs still in <c>Queued</c> can execute.
    /// </summary>
    [HttpPost("billing/export-jobs/{id:guid}/execute")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteBillingExportJobAsync(Guid id, CancellationToken cancellationToken)
    {
        await _gateway
            .SendCommandAsync<ExecuteBillingExportJobCommand, Unit>(
                new ExecuteBillingExportJobCommand(id),
                cancellationToken)
            .ConfigureAwait(false);
        // Literal location — AcceptedAtAction/AtRoute throw 500 under URL-segment API versioning.
        return Accepted($"/api/v{ApiVersionSegment}/operations/billing/export-jobs/{id}");
    }

    [HttpGet("billing/export-jobs/{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<BillingExportJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBillingExportJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _gateway
            .SendQueryAsync<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>(
                new GetBillingExportJobByIdQuery(id),
                cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : OkResource(dto, LinkCapabilitiesIndex());
    }

    /// <summary>
    /// Operator-dashboard list of recent billing export jobs. Optionally filtered to one
    /// status (<c>Queued</c>, <c>Completed</c>, <c>Failed</c>); bounded by <paramref name="take"/>
    /// (1–500, default 100). Newest-first by submission timestamp.
    /// </summary>
    [HttpGet("billing/export-jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<BillingExportJobRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListBillingExportJobsAsync(
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _gateway
                .SendQueryAsync<ListBillingExportJobsQuery, IReadOnlyList<BillingExportJobRow>>(
                    new ListBillingExportJobsQuery(status, take),
                    cancellationToken)
                .ConfigureAwait(false);
            return Ok(rows);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed record AssignStaffRoleBody
    {
        public AssignStaffRoleBody(string RoleCode) => this.RoleCode = RoleCode;
        public string RoleCode { get; init; }
        public void Deconstruct(out string roleCode) => roleCode = RoleCode;
    }

    public sealed record InventoryMovementBody
    {
        public InventoryMovementBody(int DeltaQuantity) => this.DeltaQuantity = DeltaQuantity;
        public int DeltaQuantity { get; init; }
        public void Deconstruct(out int deltaQuantity) => deltaQuantity = DeltaQuantity;
    }

    public sealed record SubmitBillingExportJobResponse
    {
        public SubmitBillingExportJobResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }
}
