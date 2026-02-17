using Asp.Versioning;

using Dialysis.DeviceIngestion.Features.Vitals.Ingest;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Vitals;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/vitals")]
public sealed class VitalsController : ControllerBase
{
    private readonly ISender _sender;

    public VitalsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Ingest vitals (BP, heart rate, weight) and create FHIR Observations.
    /// Include X-Tenant-Id header for multi-tenancy.
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestVitalsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngestVitalsResponse>> Ingest(
        [FromBody] IngestVitalsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await IngestVitalsEndpoint.HandleAsync(request, _sender, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
