using Dialysis.Treatment.Api.Contracts;
using Dialysis.Treatment.Application.Features.IngestOruMessage;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Treatment.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;

    public Hl7Controller(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("oru")]
    [ProducesResponseType(typeof(IngestOruMessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> IngestOruAsync(
        [FromBody] IngestOruMessageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IngestOruMessageCommand(request.RawHl7Message);
        IngestOruMessageResponse response = await _sender.SendAsync(command, cancellationToken);
        return Ok(response);
    }
}
