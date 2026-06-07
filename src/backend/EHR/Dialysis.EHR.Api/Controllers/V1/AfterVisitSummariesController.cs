using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.PatientPortal.Features.AuthorAfterVisitSummary;
using Dialysis.EHR.PatientPortal.Features.ListAfterVisitSummaries;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// After-visit summaries — the patient-friendly "what happened + what to do" record. Clinicians draft,
/// add instruction / follow-up / resource lines, and publish (provider routes, permission-gated);
/// patients read their own published summaries (patient routes, identity-claim gated).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/after-visit-summaries")]
public sealed class AfterVisitSummariesController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly EhrPortalAccess _portalAccess;

    public AfterVisitSummariesController(ICqrsGateway gateway, EhrPortalAccess portalAccess)
    {
        _gateway = gateway;
        _portalAccess = portalAccess;
    }

    private bool IsSelf(Guid patientId) => _portalAccess.CanActAs(User, patientId);

    /// <summary>Clinician starts a draft after-visit summary.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAfterVisitSummaryCommand body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<CreateAfterVisitSummaryCommand, Guid>(body, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/after-visit-summaries/{id}", new { id });
    }

    /// <summary>Adds an instruction / follow-up / resource line to a draft.</summary>
    [HttpPost("{summaryId:guid}/lines")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddLineAsync(Guid summaryId, [FromBody] AddLineRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var lineId = await _gateway.SendCommandAsync<AddAfterVisitSummaryLineCommand, Guid>(
            new AddAfterVisitSummaryLineCommand(summaryId, body.Kind, body.Text, body.Url), cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/after-visit-summaries/{summaryId}", new { id = lineId });
    }

    /// <summary>Publishes a draft to the patient portal.</summary>
    [HttpPost("{summaryId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishAsync(Guid summaryId, CancellationToken cancellationToken)
    {
        await _gateway.SendCommandAsync<PublishAfterVisitSummaryCommand, Unit>(
            new PublishAfterVisitSummaryCommand(summaryId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Patient lists their own published after-visit summaries.</summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AfterVisitSummaryView>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMineAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId)) return Forbid();
        var rows = await _gateway.SendQueryAsync<ListMyAfterVisitSummariesQuery, IReadOnlyList<AfterVisitSummaryView>>(
            new ListMyAfterVisitSummariesQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Patient reads one of their own published summaries.</summary>
    [HttpGet("patients/{patientId:guid}/{summaryId:guid}")]
    [ProducesResponseType(typeof(AfterVisitSummaryView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMineAsync(Guid patientId, Guid summaryId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId)) return Forbid();
        var view = await _gateway.SendQueryAsync<GetAfterVisitSummaryByIdQuery, AfterVisitSummaryView?>(
            new GetAfterVisitSummaryByIdQuery(summaryId), cancellationToken).ConfigureAwait(false);
        if (view is null) return NotFound();
        if (view.PatientId != patientId) return Forbid();
        return Ok(view);
    }

    /// <summary>Add-line request body.</summary>
    public sealed record AddLineRequest(AfterVisitLineKind Kind, string Text, string? Url);
}
