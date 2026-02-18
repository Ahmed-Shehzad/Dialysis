using Dialysis.Prescription.Api.Contracts;
using Dialysis.Prescription.Application.Features.IngestRspK22Message;
using Dialysis.Prescription.Application.Features.ProcessQbpD01Query;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Prescription.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;

    public Hl7Controller(ISender sender)
    {
        _sender = sender;
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
        var command = new IngestRspK22MessageCommand(request.RawHl7Message, request.ValidationContext);
        IngestRspK22MessageResponse response = await _sender.SendAsync(command, cancellationToken);
        return Ok(response);
    }
}
