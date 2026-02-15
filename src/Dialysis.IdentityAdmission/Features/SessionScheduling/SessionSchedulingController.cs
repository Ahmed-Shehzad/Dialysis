using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.IdentityAdmission.Features.SessionScheduling;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sessions")]
[Authorize(Policy = "Write")]
public sealed class SessionSchedulingController : ControllerBase
{
    private readonly ISender _sender;

    public SessionSchedulingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }
}
