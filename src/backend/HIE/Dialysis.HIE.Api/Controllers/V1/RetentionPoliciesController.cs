using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Documents.Features.Retention;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers.V1;

/// <summary>
/// Operator-facing CRUD for per-kind retention windows on HIE Documents. The DPO sets a
/// window per <c>DocumentReference.Kind</c>; the <c>RetentionPurgerHostedService</c> walks
/// these and purges expired documents on its 24-hour tick.
///
/// No windows are seeded — the purger is a no-op until the DPO has reviewed the clinic's
/// privacy policy and adopted a set. See <c>docs/compliance/retention.md</c> for the
/// conservative defaults matching BDSG §22 + HGB §257.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents/retention")]
[Authorize]
public sealed class RetentionPoliciesController(ICqrsGateway cqrs) : ControllerBase
{
    [HttpGet("policies")]
    [PhiAccess("hie.documents.retention.list")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<RetentionPolicyRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await cqrs.SendQueryAsync<ListRetentionPoliciesQuery, IReadOnlyList<RetentionPolicyRow>>(
            new ListRetentionPoliciesQuery(), cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        return Ok(new ResourceEnvelope<IReadOnlyList<RetentionPolicyRow>>(rows, [new LinkDto("self", self, "GET")]));
    }

    [HttpPut("policies/{kind}")]
    [PhiAccess("hie.documents.retention.upsert")]
    [ProducesResponseType(typeof(ResourceEnvelope<UpsertedPolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAsync(string kind, [FromBody] UpsertRetentionPolicyBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await cqrs.SendCommandAsync<UpsertRetentionPolicyCommand, Guid>(
            new UpsertRetentionPolicyCommand(kind, body.RetentionDays, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/documents/retention/policies";
        return Ok(new ResourceEnvelope<UpsertedPolicyDto>(
            new UpsertedPolicyDto(id, kind, body.RetentionDays),
            [new LinkDto("self", self, "GET")]));
    }

    [HttpDelete("policies/{kind}")]
    [PhiAccess("hie.documents.retention.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(string kind, CancellationToken cancellationToken)
    {
        await cqrs.SendCommandAsync<DeleteRetentionPolicyCommand, Unit>(
            new DeleteRetentionPolicyCommand(kind), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    public sealed record UpsertRetentionPolicyBody(int RetentionDays);
    public sealed record UpsertedPolicyDto(Guid Id, string Kind, int RetentionDays);
}
