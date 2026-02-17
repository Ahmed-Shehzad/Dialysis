using Asp.Versioning;
using Dialysis.Domain.Entities;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.VascularAccess;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/vascular-access")]
public sealed class VascularAccessController : ControllerBase
{
    private readonly ISender _sender;

    public VascularAccessController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create vascular access. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VascularAccessDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateVascularAccessRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new CreateVascularAccessCommand(request.PatientId, request.Type, request.Side, request.PlacementDate, request.Notes),
            cancellationToken);

        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(Get), new { id = result.Dto!.Id }, result.Dto);
    }

    /// <summary>
    /// Get vascular access by ID. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VascularAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new GetVascularAccessQuery(id), cancellationToken);
        if (result.InvalidId)
            return BadRequest(new { error = "Invalid ID format." });
        if (result.NotFound)
            return NotFound();

        return Ok(result.Dto);
    }

    /// <summary>
    /// List vascular access for a patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VascularAccessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string patientId,
        [FromQuery] VascularAccessStatus? status,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new ListVascularAccessQuery(patientId, status), cancellationToken);
        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        return Ok(result.Items);
    }

    /// <summary>
    /// Update vascular access status. Include X-Tenant-Id header.
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(VascularAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new UpdateVascularAccessStatusCommand(id, request.Status, request.Notes),
            cancellationToken);

        if (result.NotFound)
            return result.Error is not null ? BadRequest(new { error = result.Error }) : NotFound();

        return Ok(result.Dto);
    }
}

public sealed record CreateVascularAccessRequest(string PatientId, VascularAccessType Type, string? Side = null, DateTime? PlacementDate = null, string? Notes = null);
public sealed record UpdateStatusRequest(VascularAccessStatus Status, string? Notes = null);
public sealed record VascularAccessDto(string Id, string PatientId, string Type, string? Side, DateTime? PlacementDate, string Status, string? Notes, DateTime CreatedAtUtc);
