using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.DeviceIngestion.Features.IngestVitals;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/vitals")]
[Authorize(Policy = "Write")]
public sealed class VitalsIngestionController : ControllerBase
{
    private readonly ISender _sender;

    public VitalsIngestionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestVitalsCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }
}
