using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HisIntegration.Features.AdtSync;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/adt")]
[Authorize(Policy = "Write")]
public sealed class AdtController : ControllerBase
{
    private readonly ISender _sender;

    public AdtController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] AdtMessagePayload payload, CancellationToken cancellationToken)
    {
        var command = new AdtIngestCommand { MessageType = payload.MessageType, RawMessage = payload.RawMessage };
        var result = await _sender.SendAsync(command, cancellationToken);

        if (!result.Processed && result.Message?.Contains("Invalid") == true)
        {
            return BadRequest(new { Error = result.Message });
        }

        return Ok(new { result.Processed, result.PatientId, result.EncounterId, result.Message });
    }
}

public sealed record AdtMessagePayload
{
    public required string MessageType { get; init; }
    public required string RawMessage { get; init; }
}
