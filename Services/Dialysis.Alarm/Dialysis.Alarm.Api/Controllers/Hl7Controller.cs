using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Api.Contracts;
using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Features.IngestOruR40Message;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Alarm.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;
    private readonly IOraR41Builder _oraBuilder;

    public Hl7Controller(ISender sender, IAuditRecorder audit, ITenantContext tenant, IOraR41Builder oraBuilder)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
        _oraBuilder = oraBuilder;
    }

    [HttpPost("alarm")]
    [Authorize(Policy = "AlarmWrite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/x-hl7-v2+er7")]
    public async Task<IActionResult> IngestAlarmAsync(
        [FromBody] IngestOruR40MessageRequest request,
        CancellationToken cancellationToken)
    {
        string messageControlId = ExtractMsh10(request.RawHl7Message) ?? Ulid.NewUlid().ToString();

        var command = new IngestOruR40MessageCommand(request.RawHl7Message);
        IngestOruR40MessageResponse response = await _sender.SendAsync(command, cancellationToken);
        var resourceId = response.AlarmIds.Count > 0 ? string.Join(",", response.AlarmIds) : null;
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Alarm", resourceId, User.Identity?.Name,
            AuditOutcome.Success, "HL7 ORU-R40 alarm ingest", _tenant.TenantId), cancellationToken);

        string ora = _oraBuilder.BuildAccept(messageControlId);
        return Content(ora, "application/x-hl7-v2+er7");
    }

    private static string? ExtractMsh10(string hl7Message)
    {
        string normalized = hl7Message.Replace("\r\n", "\r").Replace("\n", "\r");
        string[] segments = normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);
        foreach (string seg in segments)
        {
            if (!seg.StartsWith("MSH|", StringComparison.Ordinal)) continue;
            string[] fields = seg.Split('|');
            return fields.Length > 9 && !string.IsNullOrWhiteSpace(fields[9]) ? fields[9].Trim() : null;
        }
        return null;
    }
}
