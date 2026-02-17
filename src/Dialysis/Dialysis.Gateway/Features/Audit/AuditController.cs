using Asp.Versioning;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Audit;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;

    public AuditController(ISender sender) => _sender = sender;

    /// <summary>
    /// Record an audit event. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AuditEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Record([FromBody] RecordAuditRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new RecordAuditCommand(request.Action, request.ResourceType, request.Actor, request.ResourceId, request.PatientId, request.Details),
            cancellationToken);

        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        return Created(string.Empty, result.Dto);
    }

    /// <summary>
    /// Query audit events. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query(
        [FromQuery] string? patientId,
        [FromQuery] string? resourceType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new QueryAuditQuery(patientId, resourceType, action, from, to, limit, offset),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record RecordAuditRequest(string Action, string ResourceType, string? Actor = null, string? ResourceId = null, string? PatientId = null, string? Details = null);
public sealed record AuditEventDto(string Id, string Actor, string Action, string ResourceType, string? ResourceId, string? PatientId, DateTime CreatedAtUtc, string? Details);
