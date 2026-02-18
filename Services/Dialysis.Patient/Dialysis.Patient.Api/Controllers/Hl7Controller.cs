using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Patient.Api.Contracts;
using Dialysis.Patient.Application.Features.IngestRspK22;
using Dialysis.Patient.Application.Features.ProcessQbpQ22Query;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Patient.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public Hl7Controller(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpPost("qbp-q22")]
    [Authorize(Policy = "PatientRead")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/x-hl7-v2+er7")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessQbpQ22Async(
        [FromBody] ProcessQbpQ22Request request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessQbpQ22QueryCommand(request.RawHl7Message);
        ProcessQbpQ22QueryResponse response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Patient", null, User.Identity?.Name,
            AuditOutcome.Success, "QBP^Q22 patient demographics query", _tenant.TenantId), cancellationToken);
        return Content(response.RspK22Message, "application/x-hl7-v2+er7");
    }

    [HttpPost("rsp-k22")]
    [Authorize(Policy = "PatientWrite")]
    [ProducesResponseType(typeof(IngestRspK22Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestRspK22Async(
        [FromBody] IngestRspK22Request request,
        CancellationToken cancellationToken)
    {
        var command = new IngestRspK22Command(request.RawHl7Message);
        IngestRspK22Response response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Patient", null, User.Identity?.Name,
            AuditOutcome.Success, $"RSP^K22 ingest: {response.IngestedCount} patients", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }
}
