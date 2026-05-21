using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alarms")]
public sealed class AlarmsController(ICqrsGateway gateway) : ControllerBase
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
}
