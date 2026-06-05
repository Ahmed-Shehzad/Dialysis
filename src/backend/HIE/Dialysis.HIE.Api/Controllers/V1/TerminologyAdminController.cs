using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Inbound.Terminology;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers.V1;

/// <summary>
/// Authoring + versioning surface for the platform's governed terminology (CodeSystem / ValueSet /
/// ConceptMap). The terminology lead drafts a resource, activates it, and the
/// <c>TerminologyCatalogLoader</c> overlays every <c>active</c> resource onto the in-memory catalog at
/// host startup so it serves via <c>$validate-code</c> / <c>$expand</c> / <c>$translate</c> alongside
/// the built-ins. A new version is a new (url, version) row; activating it takes effect on restart.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/terminology")]
[Authorize]
public sealed class TerminologyAdminController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;

    /// <summary>Creates the controller over the CQRS gateway.</summary>
    public TerminologyAdminController(ICqrsGateway cqrs) => _cqrs = cqrs;

    /// <summary>Lists every authored terminology resource (all versions + statuses).</summary>
    [HttpGet("resources")]
    [PhiAccess(HiePermissions.TerminologyView)]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<AuthoredTerminologyRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _cqrs.SendQueryAsync<ListAuthoredTerminologyQuery, IReadOnlyList<AuthoredTerminologyRow>>(
            new ListAuthoredTerminologyQuery(), cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        return Ok(new ResourceEnvelope<IReadOnlyList<AuthoredTerminologyRow>>(rows, [new LinkDto("self", self, "GET")]));
    }

    /// <summary>Creates or revises an authored (url, version) resource.</summary>
    [HttpPost("resources")]
    [PhiAccess(HiePermissions.TerminologyAuthor)]
    [ProducesResponseType(typeof(ResourceEnvelope<AuthoredTerminologyIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertAsync([FromBody] UpsertTerminologyBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            var id = await _cqrs.SendCommandAsync<UpsertAuthoredTerminologyCommand, Guid>(
                new UpsertAuthoredTerminologyCommand(
                    body.ResourceType, body.Url, body.Version, body.Status ?? "draft",
                    body.Name, body.FhirJson, User.Identity?.Name ?? "operator"),
                cancellationToken).ConfigureAwait(false);
            var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/terminology/resources";
            return Ok(new ResourceEnvelope<AuthoredTerminologyIdDto>(
                new AuthoredTerminologyIdDto(id), [new LinkDto("self", self, "GET")]));
        }
        catch (ArgumentException ex)
        {
            // Malformed/mismatched FhirJson or invalid metadata — fail closed with a 400.
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Transitions a resource's lifecycle status (draft → active → retired).</summary>
    [HttpPost("resources/{id:guid}/status")]
    [PhiAccess(HiePermissions.TerminologyAuthor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetStatusAsync(Guid id, [FromBody] SetTerminologyStatusBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            await _cqrs.SendCommandAsync<SetAuthoredTerminologyStatusCommand, Unit>(
                new SetAuthoredTerminologyStatusCommand(id, body.Status, User.Identity?.Name ?? "operator"),
                cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Deletes an authored resource version.</summary>
    [HttpDelete("resources/{id:guid}")]
    [PhiAccess(HiePermissions.TerminologyAuthor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _cqrs.SendCommandAsync<DeleteAuthoredTerminologyCommand, Unit>(
            new DeleteAuthoredTerminologyCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Create/revise request body.</summary>
    public sealed record UpsertTerminologyBody(
        string ResourceType, string Url, string Version, string? Status, string Name, string FhirJson);

    /// <summary>Status-transition request body.</summary>
    public sealed record SetTerminologyStatusBody(string Status);

    /// <summary>Identifier response.</summary>
    public sealed record AuthoredTerminologyIdDto(Guid Id);
}
