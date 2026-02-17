using Asp.Versioning;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Alerts;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly ISender _sender;

    public AlertsController(ISender sender) => _sender = sender;

    /// <summary>
    /// List alerts, optionally filtered by patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? patientId,
        [FromQuery] bool? activeOnly,
        [FromQuery] int? limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new ListAlertsQuery(patientId, activeOnly, limit, offset), cancellationToken);
        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        return Ok(result.Items);
    }

    /// <summary>
    /// Get a single alert by ID. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new GetAlertQuery(id), cancellationToken);
        if (result.InvalidId)
            return BadRequest(new { error = "Invalid alert ID format." });
        if (result.NotFound)
            return NotFound();

        return Ok(result.Dto);
    }

    /// <summary>
    /// Acknowledge an alert. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(string id, [FromBody] AcknowledgeAlertRequest? request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new AcknowledgeAlertCommand(id, request?.AcknowledgedBy), cancellationToken);
        if (result.InvalidId)
            return BadRequest(new { error = "Invalid alert ID format." });
        if (result.NotFound)
            return NotFound();

        return Ok(result.Dto);
    }
}

public sealed record AlertDto(string Id, string PatientId, string? ObservationId, string Severity, string Message, string Status, DateTime CreatedAtUtc, DateTime? AcknowledgedAtUtc, string? AcknowledgedBy);
public sealed record AcknowledgeAlertRequest(string? AcknowledgedBy);
