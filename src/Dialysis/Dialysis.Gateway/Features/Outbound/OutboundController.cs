using Asp.Versioning;

using Dialysis.Gateway.Features.Outbound.PushToEhr;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Outbound;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/outbound")]
public sealed class OutboundController : ControllerBase
{
    private readonly ISender _sender;

    public OutboundController(ISender sender) => _sender = sender;

    /// <summary>
    /// Push a patient's data (Patient + Observations + Procedures) to the configured EHR FHIR endpoint.
    /// Include X-Tenant-Id header. Requires Integration:EhrFhirBaseUrl to be set.
    /// </summary>
    [HttpPost("ehr/push/{patientId}")]
    [ProducesResponseType(typeof(PushResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PushToEhr(string patientId, CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var command = new PushToEhrCommand(baseUrl, patientId);
        var result = await _sender.SendAsync(command, cancellationToken);

        if (!result.Success && result.StatusCode == 400)
            return BadRequest(new { error = result.ErrorMessage });

        if (!result.Success && result.StatusCode == 404)
            return NotFound();

        return Ok(new PushResultDto(
            result.Success,
            result.StatusCode,
            result.ErrorMessage,
            result.PatientId,
            result.ResourceCount));
    }
}

public sealed record PushResultDto(bool Success, int? StatusCode, string? ErrorMessage, string PatientId, int ResourceCount);
