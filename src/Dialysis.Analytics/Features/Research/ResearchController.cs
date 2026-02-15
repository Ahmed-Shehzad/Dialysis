using Asp.Versioning;
using Dialysis.Analytics.Features.Cohorts;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Analytics.Features.Research;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/research")]
[Authorize(Policy = "Research")]
public sealed class ResearchController : ControllerBase
{
    private readonly ISender _sender;

    public ResearchController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Export cohort data for research. Supports de-identification when PublicHealthBaseUrl is configured. Requires dialysis.research scope.</summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? cohortId = null,
        [FromQuery] string resourceType = "Patient",
        [FromQuery] string level = "Basic",
        [FromBody] CohortCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(cohortId) && criteria == null)
            return BadRequest(new { error = "Provide cohortId or criteria in body" });

        var output = new MemoryStream();
        var result = await _sender.SendAsync(
            new ResearchExportCommand(cohortId, criteria, resourceType, level, output),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        output.Position = 0;
        Response.ContentType = "application/ndjson";
        Response.Headers.Append("Content-Disposition", "attachment; filename=\"research-export.ndjson\"");
        await output.CopyToAsync(Response.Body, cancellationToken);
        return new EmptyResult();
    }
}
