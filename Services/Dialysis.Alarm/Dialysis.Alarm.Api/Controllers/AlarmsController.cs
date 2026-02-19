using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Features.GetAlarms;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

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
        var query = new GetAlarmsQuery(DeviceId: deviceId, SessionId: sessionId, FromUtc: from, ToUtc: to);
        GetAlarmsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Alarm", null, User.Identity?.Name,
            AuditOutcome.Success, $"List alarms ({response.Alarms.Count})", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("fhir")]
    [Authorize(Policy = "AlarmRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlarmsFhirAsync(
        [FromQuery(Name = "_id")] string? id = null,
        [FromQuery] string? deviceId = null,
        [FromQuery] string? sessionId = null,
        [FromQuery] DateTimeOffset? date = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from ?? (date.HasValue ? date.Value.Date : (DateTimeOffset?)null);
        var toUtc = to ?? (date.HasValue ? date.Value.Date.AddDays(1).AddTicks(-1) : (DateTimeOffset?)null);
        var query = new GetAlarmsQuery(id, deviceId, sessionId, fromUtc, toUtc);
        GetAlarmsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Alarm", null, User.Identity?.Name,
            AuditOutcome.Success, $"FHIR alarms ({response.Alarms.Count})", _tenant.TenantId), cancellationToken);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = response.Alarms
                .Select(a =>
                {
                    var input = new AlarmMappingInput(
                        a.AlarmType,
                        a.SourceCode,
                        a.SourceLimits,
                        a.EventPhase,
                        a.AlarmState,
                        a.ActivityState,
                        a.Priority,
                        a.InterpretationType,
                        a.AlarmType,
                        a.DeviceId,
                        a.SessionId,
                        a.OccurredAt);
                    DetectedIssue di = AlarmMapper.ToFhirDetectedIssue(input);
                    di.Id = a.Id;
                    return new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:alarm-{a.Id}",
                        Resource = di
                    };
                })
                .ToList()
        };

        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}
