using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Features.GetAlarms;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Alarm.Api.Controllers;

[ApiController]
[Route("api/alarms")]
public sealed class AlarmsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public AlarmsController(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet]
    [Authorize(Policy = "AlarmRead")]
    [ProducesResponseType(typeof(GetAlarmsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlarmsAsync(
        [FromQuery] string? deviceId,
        [FromQuery] string? sessionId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = new GetAlarmsQuery(deviceId, sessionId, from, to);
        GetAlarmsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Alarm", null, User.Identity?.Name,
            AuditOutcome.Success, $"List alarms ({response.Alarms.Count})", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }
}
