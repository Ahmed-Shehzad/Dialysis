using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HisIntegration.Features.Hl7Streaming;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/hl7")]
[Authorize(Policy = "Write")]
public sealed class Hl7StreamingController : ControllerBase
{
    private readonly ISender _sender;

    public Hl7StreamingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("stream")]
    public async Task<IActionResult> Stream([FromBody] Hl7StreamPayload payload, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var command = new Hl7StreamIngestCommand
        {
            RawMessage = payload.RawMessage,
            MessageType = payload.MessageType,
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId
        };

        var result = await _sender.SendAsync(command, cancellationToken);

        if (!result.Processed && !string.IsNullOrEmpty(result.Error))
            return BadRequest(new { Error = result.Error });

        return Ok(new
        {
            result.Processed,
            result.PatientId,
            result.EncounterId,
            result.ResourceIds
        });
    }
}

public sealed record Hl7StreamPayload
{
    public required string RawMessage { get; init; }
    public string? MessageType { get; init; }
}
