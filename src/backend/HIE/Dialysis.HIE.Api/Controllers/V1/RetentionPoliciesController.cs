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
public sealed class RetentionPoliciesController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;
    /// <summary>
    /// Operator-facing CRUD for per-kind retention windows on HIE Documents. The DPO sets a
    /// window per <c>DocumentReference.Kind</c>; the <c>RetentionPurgerHostedService</c> walks
    /// these and purges expired documents on its 24-hour tick.
    ///
    /// No windows are seeded — the purger is a no-op until the DPO has reviewed the clinic's
    /// privacy policy and adopted a set. See <c>docs/compliance/retention.md</c> for the
    /// conservative defaults matching BDSG §22 + HGB §257.
    /// </summary>
    public RetentionPoliciesController(ICqrsGateway cqrs) => _cqrs = cqrs;
    [HttpGet("policies")]
    [PhiAccess("hie.documents.retention.list")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<RetentionPolicyRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _cqrs.SendQueryAsync<ListRetentionPoliciesQuery, IReadOnlyList<RetentionPolicyRow>>(
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
        var id = await _cqrs.SendCommandAsync<UpsertRetentionPolicyCommand, Guid>(
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
        await _cqrs.SendCommandAsync<DeleteRetentionPolicyCommand, Unit>(
            new DeleteRetentionPolicyCommand(kind), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    public sealed record UpsertRetentionPolicyBody
    {
        public UpsertRetentionPolicyBody(int RetentionDays) => this.RetentionDays = RetentionDays;
        public int RetentionDays { get; init; }
        public void Deconstruct(out int retentionDays) => retentionDays = this.RetentionDays;
    }

    public sealed record UpsertedPolicyDto
    {
        public UpsertedPolicyDto(Guid Id, string Kind, int RetentionDays)
        {
            this.Id = Id;
            this.Kind = Kind;
            this.RetentionDays = RetentionDays;
        }
        public Guid Id { get; init; }
        public string Kind { get; init; }
        public int RetentionDays { get; init; }
        public void Deconstruct(out Guid id, out string kind, out int retentionDays)
        {
            id = this.Id;
            kind = this.Kind;
            retentionDays = this.RetentionDays;
        }
    }
}
