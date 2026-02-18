using Dialysis.Alarm.Api.Contracts;
using Dialysis.Alarm.Application.Features.IngestOruR40Message;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Alarm.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;

    public Hl7Controller(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("alarm")]
    [ProducesResponseType(typeof(IngestOruR40MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> IngestAlarm(
        [FromBody] IngestOruR40MessageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IngestOruR40MessageCommand(request.RawHl7Message);
        var response = await _sender.SendAsync(command, cancellationToken);
        return Ok(response);
    }
}
