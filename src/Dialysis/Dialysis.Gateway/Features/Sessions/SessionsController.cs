using Asp.Versioning;

using Dialysis.Gateway.Features.Fhir.Session;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Sessions;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISender _sender;

    public SessionsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Start a dialysis session. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(
        [FromBody] StartSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
            return BadRequest(new { error = "PatientId is required." });

        var result = await _sender.SendAsync(
            new StartSessionCommand(request.PatientId, request.AccessSite, request.EncounterId),
            cancellationToken);

        var dto = ToDto(result.Session);
        return CreatedAtAction(nameof(Get), new { id = result.Session.Id.ToString() }, dto);
    }

    /// <summary>
    /// Get a session by ID. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default)
    {
        var session = await _sender.SendAsync(new GetSessionQuery(id), cancellationToken);
        if (session is null)
            return NotFound();

        return Ok(ToDto(session));
    }

    /// <summary>
    /// List sessions for a patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? patientId,
        [FromQuery] int? limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { error = "patientId query parameter is required." });

        var sessions = await _sender.SendAsync(
            new SearchSessionsQuery(patientId, limit, offset),
            cancellationToken);

        return Ok(sessions.Select(ToDto).ToList());
    }

    /// <summary>
    /// Complete a session with optional UF removed. Include X-Tenant-Id header.
    /// </summary>
    [HttpPut("{id}/complete")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        string id,
        [FromBody] CompleteSessionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new CompleteSessionCommand(id, request?.UfRemovedKg),
            cancellationToken);

        if (result.Session is null)
            return NotFound();

        return Ok(ToDto(result.Session));
    }

    /// <summary>
    /// Update ultrafiltration removed during session. Include X-Tenant-Id header.
    /// </summary>
    [HttpPatch("{id}/uf")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUf(
        string id,
        [FromBody] UpdateUfRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(
            new UpdateUfCommand(id, request.UfRemovedKg),
            cancellationToken);

        if (result.Session is null)
            return NotFound();

        return Ok(ToDto(result.Session));
    }

    private static SessionDto ToDto(Domain.Aggregates.Session s) => new(
        s.Id.ToString(),
        s.PatientId.Value,
        s.StartedAt,
        s.EndedAt,
        s.AccessSite,
        s.EncounterId,
        s.UfRemovedKg,
        s.Status.ToString());
}

public sealed record StartSessionRequest(string PatientId, string? AccessSite = null, string? EncounterId = null);

public sealed record CompleteSessionRequest(decimal? UfRemovedKg);

public sealed record UpdateUfRequest(decimal UfRemovedKg);

public sealed record SessionDto(
    string Id,
    string PatientId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string? AccessSite,
    string? EncounterId,
    decimal? UfRemovedKg,
    string Status);
