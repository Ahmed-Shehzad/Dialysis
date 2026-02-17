using Asp.Versioning;
using Dialysis.DeviceIngestion.Features.Hl7.Stream;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Hl7;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/hl7")]
public sealed class FailedHl7Controller : ControllerBase
{
    private readonly ISender _sender;

    public FailedHl7Controller(ISender sender) => _sender = sender;

    /// <summary>
    /// List failed HL7 messages (DLQ). Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("failed")]
    [ProducesResponseType(typeof(IReadOnlyList<FailedHl7MessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFailed(int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new ListFailedHl7Query(limit, offset), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retry a failed HL7 message by ID. Re-sends the raw message for processing.
    /// </summary>
    [HttpPost("failed/{id}/retry")]
    [ProducesResponseType(typeof(Hl7StreamResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryFailed(string id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new RetryFailedHl7Command(id), cancellationToken);

        if (result.NotFound)
            return NotFound();

        return Accepted(result.Response);
    }
}

public sealed record FailedHl7MessageDto(string Id, string? MessageControlId, string ErrorMessage, DateTime FailedAtUtc, int RetryCount);
