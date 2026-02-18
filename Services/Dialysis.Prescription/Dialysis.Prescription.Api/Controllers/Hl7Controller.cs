using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Prescription.Api.Contracts;
using Dialysis.Prescription.Application.Features.IngestRspK22Message;
using Dialysis.Prescription.Application.Features.ProcessQbpD01Query;
using Dialysis.Prescription.Application.Options;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dialysis.Prescription.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;
    private readonly PrescriptionIngestionOptions _ingestionOptions;

    public Hl7Controller(ISender sender, IAuditRecorder audit, ITenantContext tenant, IOptions<PrescriptionIngestionOptions> ingestionOptions)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
        _ingestionOptions = ingestionOptions.Value;
    }

    [HttpPost("qbp-d01")]
    [Authorize(Policy = "PrescriptionRead")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/x-hl7-v2+er7")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessQbpD01Async(
        [FromBody] ProcessQbpD01Request request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessQbpD01QueryCommand(request.RawHl7Message);
        ProcessQbpD01QueryResponse response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Prescription", response.Mrn, User.Identity?.Name,
            AuditOutcome.Success, "QBP^D01 prescription query", _tenant.TenantId), cancellationToken);
        return Content(response.RspK22Message, "application/x-hl7-v2+er7");
    }

    [HttpPost("rsp-k22")]
    [Authorize(Policy = "PrescriptionWrite")]
    [ProducesResponseType(typeof(IngestRspK22MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestRspK22Async(
        [FromBody] IngestRspK22MessageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IngestRspK22MessageCommand(
            request.RawHl7Message,
            request.ValidationContext,
            _ingestionOptions.ConflictPolicy);
        IngestRspK22MessageResponse response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Prescription", response.OrderId, User.Identity?.Name,
            AuditOutcome.Success, "RSP^K22 prescription ingest", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }
}
