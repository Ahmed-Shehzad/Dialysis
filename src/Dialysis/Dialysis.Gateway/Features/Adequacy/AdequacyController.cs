using Asp.Versioning;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Adequacy;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/adequacy")]
public sealed class AdequacyController : ControllerBase
{
    private readonly ISender _sender;

    public AdequacyController(ISender sender) => _sender = sender;

    /// <summary>
    /// Get latest adequacy and lab values for a patient (URR, Kt/V, Hb, ferritin, TSAT, PTH, albumin, potassium).
    /// Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdequacySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string patientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { error = "patientId is required." });

        var result = await _sender.SendAsync(new GetAdequacySummaryQuery(patientId), cancellationToken);
        return Ok(result);
    }
}
