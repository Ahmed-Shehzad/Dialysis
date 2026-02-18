using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Treatment.Api.Controllers;

[ApiController]
[Route("api/treatment-sessions")]
public sealed class TreatmentSessionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public TreatmentSessionsController(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet("{sessionId}")]
    [Authorize(Policy = "TreatmentRead")]
    [ProducesResponseType(typeof(GetTreatmentSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        var query = new GetTreatmentSessionQuery(new SessionId(sessionId));
        GetTreatmentSessionResponse? response = await _sender.SendAsync(query, cancellationToken);
        if (response is not null)
            await _audit.RecordAsync(new AuditRecordRequest(
                AuditAction.Read, "TreatmentSession", sessionId, User.Identity?.Name,
                AuditOutcome.Success, "Treatment session retrieval", _tenant.TenantId), cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }
}
