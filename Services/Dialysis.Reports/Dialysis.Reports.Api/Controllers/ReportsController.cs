using BuildingBlocks.Tenancy;

using Dialysis.Reports.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Reports.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "ReportsRead")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportsAggregationService _reports;
    private readonly ITenantContext _tenant;

    public ReportsController(ReportsAggregationService reports, ITenantContext tenant)
    {
        _reports = reports;
        _tenant = tenant;
    }

    [HttpGet("sessions-summary")]
    [ProducesResponseType(typeof(SessionsSummaryReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> SessionsSummaryAsync(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var toUtc = to ?? DateTimeOffset.UtcNow;
        var report = await _reports.GetSessionsSummaryAsync(fromUtc, toUtc, _tenant.TenantId, Request, cancellationToken);
        return Ok(report);
    }

    [HttpGet("alarms-by-severity")]
    [ProducesResponseType(typeof(AlarmsBySeverityReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> AlarmsBySeverityAsync(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var toUtc = to ?? DateTimeOffset.UtcNow;
        var report = await _reports.GetAlarmsBySeverityAsync(fromUtc, toUtc, _tenant.TenantId, Request, cancellationToken);
        return Ok(report);
    }
}
