using Asp.Versioning;

using Dialysis.DeviceIngestion.Features.Hl7.Stream;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Hl7;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;

    public Hl7Controller(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Accept raw HL7 v2 ORU message from Mirth. Parses ORU^R01, extracts PID/OBX, creates Observations.
    /// Include X-Tenant-Id header for multi-tenancy.
    /// </summary>
    [HttpPost("stream")]
    [ProducesResponseType(typeof(Hl7StreamResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stream(
        [FromBody] Hl7StreamRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawMessage))
            return BadRequest(new { error = "RawMessage is required" });

        var result = await Hl7StreamEndpoint.HandleAsync(request, _sender, cancellationToken);
        return Accepted(result);
    }
}
