using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;
using Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;
using Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alarms")]
public sealed class AlarmsController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly ICurrentUser _currentUser;
    public AlarmsController(ICqrsGateway gateway, ICurrentUser currentUser)
    {
        _gateway = gateway;
        _currentUser = currentUser;
    }
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
        var alarms = await _gateway
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
        var acknowledgedBy = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(acknowledgedBy))
            return Problem(
                title: "Acknowledger identity missing.",
                detail: "The acknowledging user could not be resolved from the current authentication context.",
                statusCode: StatusCodes.Status401Unauthorized);

        await _gateway
            .SendCommandAsync<AcknowledgeAlarmCommand, Unit>(
                new AcknowledgeAlarmCommand(alarmId, acknowledgedBy), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Ingests a dialysis-machine alarm state-change over HTTP (operator / device-gateway /
    /// telemetry path), the synchronous twin of the SmartConnect alarm consumer. The first
    /// <c>Present</c> for a (machine, code) opens a new alarm; subsequent <c>Present</c> /
    /// <c>Inactivating</c> / <c>Resolved</c> states transition the same aggregate. Returns
    /// <c>202 Accepted</c> — the resulting alarm surfaces on <c>GET /alarms</c>.
    /// </summary>
    [HttpPost("machine")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RaiseMachineAlarmAsync(
        [FromBody] RaiseMachineAlarmRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            await _gateway
                .SendCommandAsync<RaiseMachineAlarmCommand, Unit>(
                    new RaiseMachineAlarmCommand(
                        request.MachineSerial,
                        request.AlarmCode,
                        request.AlarmSource,
                        request.AlarmPhase,
                        string.IsNullOrWhiteSpace(request.State) ? "Present" : request.State,
                        request.ObservedAtUtc ?? DateTime.UtcNow,
                        request.SessionId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        return Accepted();
    }

    /// <summary>Machine-alarm ingest body. <see cref="State"/> is <c>Present</c> | <c>Inactivating</c> | <c>Resolved</c>.</summary>
    public sealed record RaiseMachineAlarmRequest(
        string MachineSerial,
        long AlarmCode,
        string? AlarmSource,
        string? AlarmPhase,
        string? State,
        DateTime? ObservedAtUtc,
        Guid? SessionId);
}
