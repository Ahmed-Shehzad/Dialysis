using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Inbound.Mpi.Features;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers;

/// <summary>
/// Master Patient Index steward console: the probable-duplicate review queue and its adjudication.
/// HATEOAS-enveloped admin surface (the FHIR <c>$match</c> stays native FHIR in <c>FhirController</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hie/mpi")]
[Authorize]
public sealed class MpiAdminController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;
    /// <summary>Master Patient Index steward console.</summary>
    public MpiAdminController(ICqrsGateway cqrs) => _cqrs = cqrs;

    /// <summary>The steward's queue of suspected-duplicate pairs awaiting adjudication.</summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<PatientLinkReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReviewsAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var reviews = await _cqrs
            .SendQueryAsync<ListPendingPatientLinkReviewsQuery, IReadOnlyList<PatientLinkReviewDto>>(
                new ListPendingPatientLinkReviewsQuery(take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(new ResourceEnvelope<IReadOnlyList<PatientLinkReviewDto>>(reviews, []));
    }

    /// <summary>Adjudicate a pair: link (same person) or reject (distinct). Reviewer is the authenticated steward.</summary>
    [HttpPost("reviews/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveAsync(
        Guid id,
        [FromBody] ResolveReviewRequest body,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var reviewedBy = currentUser.UserId ?? User.Identity?.Name ?? "steward";
        try
        {
            await _cqrs.SendCommandAsync<ResolvePatientLinkReviewCommand, Dialysis.CQRS.Unit>(
                new ResolvePatientLinkReviewCommand(id, body.Link, body.Note, reviewedBy), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Steward decision: link = same person, otherwise distinct.</summary>
    public sealed record ResolveReviewRequest(bool Link, string? Note);
}
