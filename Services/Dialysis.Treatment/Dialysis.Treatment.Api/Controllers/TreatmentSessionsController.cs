using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Hl7.Fhir.Model;

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

    [HttpGet("{sessionId}/observations")]
    [Authorize(Policy = "TreatmentRead")]
    [ProducesResponseType(typeof(GetObservationsInTimeRangeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetObservationsInTimeRangeAsync(
        string sessionId,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startUtc = start ?? DateTimeOffset.UtcNow.AddHours(-4);
        DateTimeOffset endUtc = end ?? DateTimeOffset.UtcNow;
        if (startUtc > endUtc) (startUtc, endUtc) = (endUtc, startUtc);

        var query = new GetObservationsInTimeRangeQuery(sessionId, startUtc, endUtc);
        GetObservationsInTimeRangeResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "TreatmentSession", sessionId, User.Identity?.Name,
            AuditOutcome.Success, $"Time-series observations ({response.Observations.Count})", _tenant.TenantId), cancellationToken);
        return Ok(response);
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

    [HttpGet("{sessionId}/fhir")]
    [Authorize(Policy = "TreatmentRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySessionIdFhirAsync(string sessionId, CancellationToken cancellationToken)
    {
        var query = new GetTreatmentSessionQuery(new SessionId(sessionId));
        GetTreatmentSessionResponse? response = await _sender.SendAsync(query, cancellationToken);
        if (response is null)
            return NotFound();

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "TreatmentSession", sessionId, User.Identity?.Name,
            AuditOutcome.Success, "Treatment session FHIR retrieval", _tenant.TenantId), cancellationToken);

        Procedure procedure = ProcedureMapper.ToFhirProcedure(
            response.SessionId,
            response.PatientMrn,
            response.DeviceId,
            response.Status,
            response.StartedAt,
            response.EndedAt);
        procedure.Id = $"proc-{response.SessionId}";

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Identifier = new Identifier("urn:dialysis:session", response.SessionId)
        };

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            FullUrl = $"urn:uuid:procedure-{response.SessionId}",
            Resource = procedure
        });

        foreach (ObservationDto obs in response.Observations)
        {
            var input = new ObservationMappingInput(
                obs.Code,
                obs.Value,
                obs.Unit,
                obs.SubId,
                obs.ReferenceRange,
                obs.Provenance,
                obs.EffectiveTime,
                response.DeviceId,
                response.PatientMrn,
                obs.ChannelName);
            Observation fhirObs = ObservationMapper.ToFhirObservation(input);
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:obs-{obs.Code}-{obs.SubId ?? "0"}",
                Resource = fhirObs
            });
        }

        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}
