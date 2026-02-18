using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Api.Contracts;
using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Features.IngestOruMessage;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Treatment.Api.Controllers;

[ApiController]
[Route("api/hl7")]
public sealed class Hl7Controller : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;
    private readonly IAckR01Builder _ackBuilder;

    public Hl7Controller(ISender sender, IAuditRecorder audit, ITenantContext tenant, IAckR01Builder ackBuilder)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
        _ackBuilder = ackBuilder;
    }

    [HttpPost("oru")]
    [Authorize(Policy = "TreatmentWrite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/x-hl7-v2+er7")]
    public async Task<IActionResult> IngestOruAsync(
        [FromBody] IngestOruMessageRequest request,
        CancellationToken cancellationToken)
    {
        string messageControlId = ExtractMsh10(request.RawHl7Message) ?? Ulid.NewUlid().ToString();

        var command = new IngestOruMessageCommand(request.RawHl7Message);
        IngestOruMessageResponse response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Treatment", response.SessionId, User.Identity?.Name,
            AuditOutcome.Success, "HL7 ORU observation ingest", _tenant.TenantId), cancellationToken);

        string ack = _ackBuilder.BuildAccept(messageControlId);
        return Content(ack, "application/x-hl7-v2+er7");
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
