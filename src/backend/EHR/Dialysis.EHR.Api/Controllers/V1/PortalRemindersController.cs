using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.ClinicalNotes.Features.GetPatientReminders;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Patient-facing health reminders — the patient's own open quality-measure gaps mapped to
/// plain-language "things to do for your health". Gated by the caller's patient identity claim.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal/reminders")]
public sealed class PortalRemindersController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly EhrPortalAccess _portalAccess;

    public PortalRemindersController(ICqrsGateway gateway, EhrPortalAccess portalAccess)
    {
        _gateway = gateway;
        _portalAccess = portalAccess;
    }

    /// <summary>The signed-in patient's health reminders.</summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientReminderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMineAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!_portalAccess.CanActAs(User, patientId))
            return Forbid();
        var reminders = await _gateway.SendQueryAsync<GetPatientRemindersQuery, IReadOnlyList<PatientReminderDto>>(
            new GetPatientRemindersQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(reminders);
    }
}
