using Asp.Versioning;

using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Alerts;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertRepository _alertRepository;
    private readonly ITenantContext _tenantContext;

    public AlertsController(IAlertRepository alertRepository, ITenantContext tenantContext)
    {
        _alertRepository = alertRepository;
        _tenantContext = tenantContext;
    }

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
        var tenantId = _tenantContext.TenantId;

        IReadOnlyList<Alert> alerts;
        if (!string.IsNullOrWhiteSpace(patientId))
        {
            var pid = new PatientId(patientId);
            alerts = await _alertRepository.GetByPatientAsync(tenantId, pid, activeOnly, limit, offset, cancellationToken);
        }
        else
        {
            return BadRequest(new { error = "patientId query parameter is required." });
        }

        var dtos = alerts.Select(a => ToDto(a)).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get a single alert by ID. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(id, out var alertUlid))
            return BadRequest(new { error = "Invalid alert ID format." });

        var tenantId = _tenantContext.TenantId;
        var alert = await _alertRepository.GetByIdAsync(tenantId, alertUlid, cancellationToken);
        if (alert is null)
            return NotFound();

        return Ok(ToDto(alert));
    }

    /// <summary>
    /// Acknowledge an alert. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(
        string id,
        [FromBody] AcknowledgeAlertRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(id, out var alertUlid))
            return BadRequest(new { error = "Invalid alert ID format." });

        var tenantId = _tenantContext.TenantId;
        var alert = await _alertRepository.GetByIdAsync(tenantId, alertUlid, cancellationToken);
        if (alert is null)
            return NotFound();

        alert.Acknowledge(request?.AcknowledgedBy);
        await _alertRepository.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(alert));
    }

    private static AlertDto ToDto(Alert a) => new(
        a.Id.ToString(),
        a.PatientId.Value,
        a.ObservationId?.ToString(),
        a.Severity,
        a.Message,
        a.Status.ToString(),
        a.CreatedAtUtc,
        a.AcknowledgedAtUtc,
        a.AcknowledgedBy);
}

public sealed record AlertDto(
    string Id,
    string PatientId,
    string? ObservationId,
    string Severity,
    string Message,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgedBy);

public sealed record AcknowledgeAlertRequest(string? AcknowledgedBy);
