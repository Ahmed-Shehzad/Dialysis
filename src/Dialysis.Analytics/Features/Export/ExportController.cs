using Asp.Versioning;
using Dialysis.Analytics.Features.Cohorts;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Analytics.Features.Export;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/export")]
[Authorize(Policy = "Read")]
public sealed class ExportController : ControllerBase
{
    private readonly ISender _sender;

    public ExportController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Batch export FHIR resources by date range. Format: ndjson or csv.</summary>
    [HttpGet]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Export(
        [FromQuery] string resourceType = "Encounter",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string format = "ndjson",
        CancellationToken cancellationToken = default)
    {
        if (!new[] { "Patient", "Encounter", "Observation" }.Contains(resourceType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "resourceType must be Patient, Encounter, or Observation" });

        var fmt = format.Equals("csv", StringComparison.OrdinalIgnoreCase) ? ExportFormat.Csv : ExportFormat.NdJson;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);

        var contentType = fmt == ExportFormat.NdJson ? "application/ndjson" : "text/csv";
        var filename = $"{resourceType.ToLowerInvariant()}-{f:yyyyMMdd}-{t:yyyyMMdd}.{(fmt == ExportFormat.NdJson ? "ndjson" : "csv")}";

        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

        await _sender.SendAsync(new ExportCommand(resourceType, f, t, fmt, Response.Body), cancellationToken);
        return new EmptyResult();
    }

    /// <summary>Export cohort members. Pass criteria in body; returns resources for cohort patient/encounter IDs.</summary>
    [HttpPost("cohort")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> ExportCohort(
        [FromBody] CohortCriteria criteria,
        [FromQuery] string resourceType = "Patient",
        [FromQuery] string format = "ndjson",
        CancellationToken cancellationToken = default)
    {
        if (!new[] { "Patient", "Encounter" }.Contains(resourceType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "resourceType must be Patient or Encounter" });

        var fmt = format.Equals("csv", StringComparison.OrdinalIgnoreCase) ? ExportFormat.Csv : ExportFormat.NdJson;
        if (fmt == ExportFormat.Csv)
        {
            return BadRequest(new { error = "Cohort export supports ndjson only" });
        }

        var cohort = await _sender.SendAsync(new ResolveCohortQuery(criteria), cancellationToken);

        var contentType = "application/ndjson";
        var filename = $"cohort-{resourceType.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.ndjson";

        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

        await _sender.SendAsync(new ExportCohortCommand(cohort, resourceType, fmt, Response.Body), cancellationToken);
        return new EmptyResult();
    }
}
