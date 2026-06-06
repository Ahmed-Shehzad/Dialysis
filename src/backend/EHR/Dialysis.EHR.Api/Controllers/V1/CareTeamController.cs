using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.CareTeam;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>A patient's care-team roster — providers + roles, with one designated primary.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/care-team")]
public sealed class CareTeamController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public CareTeamController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Returns the patient's care team, or 204 when none exists yet.</summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(CareTeamView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var view = await _gateway.SendQueryAsync<GetCareTeamQuery, CareTeamView?>(
            new GetCareTeamQuery(patientId), cancellationToken).ConfigureAwait(false);
        return view is null ? NoContent() : Ok(view);
    }

    [HttpPost("patients/{patientId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMemberAsync(Guid patientId, [FromBody] AddMemberRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<AddCareTeamMemberCommand, Guid>(
            new AddCareTeamMemberCommand(patientId, body.ProviderId, body.Role, body.IsPrimary), cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/care-team/patients/{patientId}", new { id });
    }

    [HttpDelete("patients/{patientId:guid}/members/{providerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMemberAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken)
    {
        await _gateway.SendCommandAsync<RemoveCareTeamMemberCommand, Unit>(
            new RemoveCareTeamMemberCommand(patientId, providerId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("patients/{patientId:guid}/members/{providerId:guid}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPrimaryAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken)
    {
        await _gateway.SendCommandAsync<SetPrimaryCareTeamMemberCommand, Unit>(
            new SetPrimaryCareTeamMemberCommand(patientId, providerId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Add-member request body.</summary>
    public sealed record AddMemberRequest(Guid ProviderId, CareTeamRole Role, bool IsPrimary);
}
