using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.AuditConsent.Features.Audit;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = "Read")]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;

    public AuditController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string? resourceType, [FromQuery] string? resourceId, CancellationToken cancellationToken)
    {
        var query = new GetAuditQuery { ResourceType = resourceType, ResourceId = resourceId };
        var result = await _sender.SendAsync(query, cancellationToken);
        return Ok(result.Entries);
    }

    [HttpPost]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Record([FromBody] RecordAuditCommand command, CancellationToken cancellationToken)
    {
        await _sender.SendAsync(command, cancellationToken);
        return Ok();
    }

    /// <summary>Check if consent exists for the given resource and action (e.g. ResearchExport/cohortId, EHealthShare/patientId).</summary>
    [HttpGet("consent")]
    public async Task<IActionResult> CheckConsent(
        [FromQuery] string resourceType,
        [FromQuery] string resourceId,
        [FromQuery] string action = "consent-granted",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
            return BadRequest(new { error = "resourceType and resourceId are required" });

        var query = new CheckConsentQuery { ResourceType = resourceType, ResourceId = resourceId, Action = action };
        var result = await _sender.SendAsync(query, cancellationToken);
        return Ok(new { hasConsent = result.HasConsent });
    }
}
