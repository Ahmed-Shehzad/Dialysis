using Asp.Versioning;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Cohorts;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/cohorts")]
public sealed class CohortsController : ControllerBase
{
    private readonly ISender _sender;

    public CohortsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Query cohort of patients by criteria. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("query")]
    [ProducesResponseType(typeof(CohortResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query(
        [FromQuery] bool? hasActiveAlert,
        [FromQuery] DateTime? sessionFrom,
        [FromQuery] DateTime? sessionTo,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new QueryCohortQuery(hasActiveAlert, sessionFrom, sessionTo, limit, offset),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Export cohort as JSON or CSV. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] bool? hasActiveAlert,
        [FromQuery] DateTime? sessionFrom,
        [FromQuery] DateTime? sessionTo,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new ExportCohortQuery(hasActiveAlert, sessionFrom, sessionTo, format),
            cancellationToken);

        if (result.CsvContent is not null)
            return Content(result.CsvContent, result.ContentType ?? "text/csv");

        return Ok(new { patientIds = result.PatientIds, total = result.PatientIds.Count });
    }
}
