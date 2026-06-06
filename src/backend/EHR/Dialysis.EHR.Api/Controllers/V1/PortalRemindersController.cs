using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.ClinicalNotes.Features.GetPatientReminders;
using Dialysis.Module.Hosting.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly bool _authorityConfigured;

    public PortalRemindersController(ICqrsGateway gateway, IOptions<ModuleAuthenticationOptions> authOptions)
    {
        _gateway = gateway;
        _authorityConfigured = !string.IsNullOrWhiteSpace(authOptions.Value.Authority);
    }

    /// <summary>The signed-in patient's health reminders.</summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientReminderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMineAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!EhrPatientAccess.IsSelf(User, patientId, _authorityConfigured)) return Forbid();
        var reminders = await _gateway.SendQueryAsync<GetPatientRemindersQuery, IReadOnlyList<PatientReminderDto>>(
            new GetPatientRemindersQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(reminders);
    }
}
