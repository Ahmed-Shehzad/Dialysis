using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Treatment.Api.Controllers;

[ApiController]
[Route("api/treatment-sessions")]
public sealed class TreatmentSessionsController : ControllerBase
{
    private readonly ISender _sender;

    public TreatmentSessionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(GetTreatmentSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        var query = new GetTreatmentSessionQuery(new SessionId(sessionId));
        var response = await _sender.SendAsync(query, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
