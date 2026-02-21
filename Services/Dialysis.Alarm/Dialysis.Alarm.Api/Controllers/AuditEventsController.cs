using BuildingBlocks.Abstractions;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Alarm.Api.Controllers;

[ApiController]
[Route("api/alarms/audit-events")]
[Authorize(Policy = "AlarmRead")]
public sealed class AuditEventsController : ControllerBase
{
    private readonly IAuditEventStore _store;

    public AuditEventsController(IAuditEventStore store) => _store = store;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditEventsAsync([FromQuery] int count = 100, CancellationToken cancellationToken = default)
    {
        const int maxCount = 500;
        int take = Math.Clamp(count, 1, maxCount);

        IReadOnlyList<AuditRecordRequest> records = await _store.GetRecentAsync(take, cancellationToken);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = records
                .Select((r, i) => new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:audit-{i}-{Guid.NewGuid():N}",
                    Resource = AuditEventMapper.ToFhirAuditEvent(r, "dialysis-alarm-api")
                })
                .ToList()
        };

        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}
