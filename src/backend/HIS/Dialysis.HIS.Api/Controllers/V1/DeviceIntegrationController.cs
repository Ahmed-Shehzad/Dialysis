using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Patient monitoring</em> — device / telemetry ingest path (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration")]
public sealed class DeviceIntegrationController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("device-readings")]
    [EnableRateLimiting("DeviceIngest")]
    [ProducesResponseType(typeof(ResourceEnvelope<IngestDeviceReadingResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> IngestDeviceReadingAsync([FromBody] IngestDeviceReadingCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<IngestDeviceReadingCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new IngestDeviceReadingResponse(id), LinkCapabilitiesIndex());
    }

    public sealed record IngestDeviceReadingResponse(Guid Id);
}
