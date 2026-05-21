using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;
using Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alarms")]
public sealed class AlarmsController(ICqrsGateway gateway, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Returns every alarm currently <c>Present</c> or <c>Inactivating</c>, ordered by first
    /// observation. Drives the chairside alarm strip and any future operator alarm board.
    /// Polling the endpoint at a few-second cadence is the demo path; a SignalR push channel
    /// is the natural follow-up.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveAlarmDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActiveAsync(CancellationToken cancellationToken)
    {
        var alarms = await gateway
            .SendQueryAsync<ListActiveAlarmsQuery, IReadOnlyList<ActiveAlarmDto>>(
                new ListActiveAlarmsQuery(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(alarms);
    }

    /// <summary>
    /// Records that the authenticated clinician has acknowledged the given alarm. The
    /// acknowledger is sourced from <see cref="ICurrentUser"/> — the SPA does not get to
    /// choose who acknowledged. Idempotent: the first acknowledger wins, subsequent calls
    /// are silently absorbed by the aggregate.
    /// </summary>
    [HttpPost("{alarmId:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcknowledgeAsync(Guid alarmId, CancellationToken cancellationToken)
    {
        var acknowledgedBy = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(acknowledgedBy))
            return Problem(
                title: "Acknowledger identity missing.",
                detail: "The acknowledging user could not be resolved from the current authentication context.",
                statusCode: StatusCodes.Status401Unauthorized);

        await gateway
            .SendCommandAsync<AcknowledgeAlarmCommand, Unit>(
                new AcknowledgeAlarmCommand(alarmId, acknowledgedBy), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }
}
