using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Integration.Features.Surveillance;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Cross-patient patient-safety surveillance — adverse-event counts by kind/severity over a window,
/// with deterministic spike flags. The signal a single chart or session can't show.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/safety/surveillance")]
public sealed class SafetySurveillanceController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public SafetySurveillanceController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Adverse-event surveillance snapshot over the window (default 7 days).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SurveillanceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int windowDays = 7, [FromQuery] int take = 500, CancellationToken cancellationToken = default)
    {
        var result = await _gateway.SendQueryAsync<GetAdverseEventSurveillanceQuery, SurveillanceResult>(
            new GetAdverseEventSurveillanceQuery(windowDays, take), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
