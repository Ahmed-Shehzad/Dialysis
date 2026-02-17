using Asp.Versioning;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.SessionSummary;

/// <summary>
/// Dialysis Session Summary Publisher: builds FHIR bundles (Encounter, Observations, Procedure)
/// from completed sessions. Can POST to a FHIR endpoint or save to file for development.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/session-summary")]
public sealed class SessionSummaryController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SessionSummaryPublisher _publisher;

    public SessionSummaryController(ISender sender, SessionSummaryPublisher publisher)
    {
        _sender = sender;
        _publisher = publisher;
    }

    /// <summary>
    /// Build a FHIR session summary bundle for a completed session. Include X-Tenant-Id header.
    /// Optionally save to file when saveToPath query param is provided (e.g. for dev/testing).
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK, "application/fhir+json")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySessionId(
        string sessionId,
        [FromQuery] string? saveToPath,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var query = new GetSessionSummaryQuery(sessionId, baseUrl, saveToPath);
        var result = await _sender.SendAsync(query, cancellationToken);

        if (result.Error is not null)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return Content(result.Json!, "application/fhir+json");
    }

    /// <summary>
    /// Build a FHIR session summary bundle from a JSON mock. Useful for testing without a real session.
    /// POST body: SessionSummaryRequest (sessionId, patientId, tenantId, startedAt, endedAt, ...).
    /// </summary>
    [HttpPost("publish")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK, "application/fhir+json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishFromMock(
        [FromBody] SessionSummaryRequest request,
        [FromQuery] string? saveToPath = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.PatientId))
            return BadRequest(new { error = "SessionId and PatientId are required." });

        var startedAt = request.StartedAt ?? DateTimeOffset.UtcNow.AddHours(-4);
        var endedAt = request.EndedAt ?? DateTimeOffset.UtcNow;
        var input = new SessionSummaryInput(
            request.SessionId,
            new Dialysis.SharedKernel.ValueObjects.PatientId(request.PatientId),
            new Dialysis.SharedKernel.ValueObjects.TenantId(request.TenantId ?? "default"),
            startedAt,
            endedAt,
            request.UfRemovedKg,
            request.AccessSite,
            request.PreWeightKg,
            request.PostWeightKg,
            request.SystolicBp,
            request.DiastolicBp,
            request.Complications,
            request.IncludeProcedure);

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var bundle = _publisher.BuildBundle(input, baseUrl);

        if (!string.IsNullOrEmpty(saveToPath))
        {
            await _publisher.SaveToFileAsync(bundle, saveToPath, cancellationToken);
        }

        var json = SessionSummaryPublisher.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}

/// <summary>Request body for publishing from a JSON mock.</summary>
public sealed record SessionSummaryRequest(
    string SessionId,
    string PatientId,
    string? TenantId = "default",
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EndedAt = null,
    decimal? UfRemovedKg = null,
    string? AccessSite = null,
    decimal? PreWeightKg = null,
    decimal? PostWeightKg = null,
    int? SystolicBp = null,
    int? DiastolicBp = null,
    string? Complications = null,
    bool IncludeProcedure = true
);
