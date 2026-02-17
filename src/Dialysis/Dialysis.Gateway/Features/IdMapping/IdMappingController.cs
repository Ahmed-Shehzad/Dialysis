using Asp.Versioning;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.IdMapping;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/id-mappings")]
public sealed class IdMappingController : ControllerBase
{
    private readonly ISender _sender;

    public IdMappingController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create a cross-system ID mapping. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdMappingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateIdMappingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new CreateIdMappingCommand(request.ResourceType, request.LocalId, request.ExternalSystem, request.ExternalId),
            cancellationToken);

        if (result.Conflict)
            return Conflict(new { error = "Mapping already exists for this resource and external system." });

        var location = $"/api/v1/id-mappings/by-external?resourceType={Uri.EscapeDataString(request.ResourceType)}&externalSystem={Uri.EscapeDataString(request.ExternalSystem)}&externalId={Uri.EscapeDataString(request.ExternalId)}";
        return Created(location, result.Dto);
    }

    /// <summary>
    /// Get mapping by external system ID.
    /// </summary>
    [HttpGet("by-external")]
    [ProducesResponseType(typeof(IdMappingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExternal(
        [FromQuery] string resourceType,
        [FromQuery] string externalSystem,
        [FromQuery] string externalId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new GetIdMappingByExternalQuery(resourceType, externalSystem, externalId),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// List mappings for a resource type.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IdMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string resourceType,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new ListIdMappingsQuery(resourceType, limit, offset), cancellationToken);
        return Ok(result);
    }
}

public sealed record CreateIdMappingRequest(string ResourceType, string LocalId, string ExternalSystem, string ExternalId);
public sealed record IdMappingDto(string Id, string TenantId, string ResourceType, string LocalId, string ExternalSystem, string ExternalId, DateTime CreatedAtUtc);
