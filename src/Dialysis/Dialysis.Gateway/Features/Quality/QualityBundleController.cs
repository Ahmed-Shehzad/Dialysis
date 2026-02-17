using Asp.Versioning;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Quality;

/// <summary>
/// De-identified quality bundle for regulatory reporting (e.g. NHSN, ESRD QIP).
/// Strips patient identifiers and returns minimal Procedure/Observation data.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/quality")]
public sealed class QualityBundleController : ControllerBase
{
    private readonly ISender _sender;

    public QualityBundleController(ISender sender) => _sender = sender;

    /// <summary>
    /// Export de-identified quality bundle (Procedure + adequacy Observations) for date range.
    /// Patient identifiers are omitted. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("bundle")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> GetDeidentifiedBundle(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int limit = 500,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var query = new QualityBundleQuery(baseUrl, from, to, limit);
        var result = await _sender.SendAsync(query, cancellationToken);

        return Content(result.FhirBundleJson, "application/fhir+json");
    }
}
