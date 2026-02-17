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

    /// <summary>
    /// Ingest vitals from raw device output via adapter (Phase 1.1.4). Include X-Device-Adapter and X-Tenant-Id headers.
    /// </summary>
    [HttpPost("ingest/raw")]
    [Consumes("text/plain", "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RawIngest(
        [FromBody] string rawPayload,
        [FromHeader(Name = "X-Device-Adapter")] string adapterId = "passthrough",
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new RawIngestVitalsCommand(rawPayload, adapterId), cancellationToken);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Ok(new { observationId = result.ObservationId });
    }
}
