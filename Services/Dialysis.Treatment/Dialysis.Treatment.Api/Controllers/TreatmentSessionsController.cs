using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Hl7ToFhir;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.GetTreatmentSessions;

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

    [HttpGet("fhir")]
    [Authorize(Policy = "TreatmentRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTreatmentSessionsFhirAsync(
        [FromQuery] int limit = 500,
        [FromQuery] string? subject = null,
        [FromQuery] string? patient = null,
        [FromQuery] DateTimeOffset? date = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var mrn = !string.IsNullOrWhiteSpace(subject) ? subject : patient;
        MedicalRecordNumber? mrnVal = !string.IsNullOrWhiteSpace(mrn) ? new MedicalRecordNumber(mrn) : null;
        var from = dateFrom ?? (date.HasValue ? date.Value.Date : (DateTimeOffset?)null);
        var to = dateTo ?? (date.HasValue ? date.Value.Date.AddDays(1).AddTicks(-1) : (DateTimeOffset?)null);
        var query = new GetTreatmentSessionsQuery(Math.Min(limit, 1_000), mrnVal, from, to);
        GetTreatmentSessionsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "TreatmentSession", null, User.Identity?.Name,
            AuditOutcome.Success, $"FHIR treatment sessions ({response.Sessions.Count})", _tenant.TenantId), cancellationToken);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = []
        };

        foreach (TreatmentSessionSummary session in response.Sessions)
        {
            Procedure procedure = ProcedureMapper.ToFhirProcedure(
                session.SessionId, session.PatientMrn, session.DeviceId, session.Status,
                session.StartedAt, session.EndedAt);
            procedure.Id = $"proc-{session.SessionId}";
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:procedure-{session.SessionId}",
                Resource = procedure
            });

            foreach (ObservationDto obs in session.Observations)
            {
                string obsFullUrl = $"urn:uuid:obs-{obs.Code}-{obs.SubId ?? "0"}";
                var input = new ObservationMappingInput(
                    obs.Code, obs.Value, obs.Unit, obs.SubId, obs.ReferenceRange,
                    obs.Provenance, obs.EffectiveTime, session.DeviceId, session.PatientMrn, obs.ChannelName);
                Observation fhirObs = ObservationMapper.ToFhirObservation(input);
                bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = obsFullUrl, Resource = fhirObs });

                if (!string.IsNullOrEmpty(obs.Provenance))
                {
                    DateTimeOffset occurredAt = obs.EffectiveTime ?? session.StartedAt ?? DateTimeOffset.UtcNow;
                    Provenance prov = ProvenanceMapper.ToFhirProvenance(obsFullUrl, obs.Provenance, occurredAt, session.DeviceId);
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:prov-{obs.Code}-{obs.SubId ?? "0"}",
                        Resource = prov
                    });
                }
            }
        }

        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }

    [HttpGet("reports/summary")]
    [Authorize(Policy = "TreatmentRead")]
    [ProducesResponseType(typeof(SessionsSummaryReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionsSummaryAsync(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var toUtc = to ?? DateTimeOffset.UtcNow;
        var query = new GetTreatmentSessionsQuery(10_000, null, fromUtc, toUtc);
        GetTreatmentSessionsResponse response = await _sender.SendAsync(query, cancellationToken);
        var sessions = response.Sessions;
        decimal avgMinutes = 0;
        if (sessions.Count > 0)
        {
            var durations = sessions
                .Where(s => s.StartedAt.HasValue && s.EndedAt.HasValue)
                .Select(s => (s.EndedAt!.Value - s.StartedAt!.Value).TotalMinutes)
                .ToList();
            avgMinutes = durations.Count > 0 ? (decimal)durations.Average() : 0;
        }
        return Ok(new SessionsSummaryReport(sessions.Count, avgMinutes, fromUtc, toUtc));
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
            string obsFullUrl = $"urn:uuid:obs-{obs.Code}-{obs.SubId ?? "0"}";
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
                FullUrl = obsFullUrl,
                Resource = fhirObs
            });

            if (!string.IsNullOrEmpty(obs.Provenance))
            {
                DateTimeOffset occurredAt = obs.EffectiveTime ?? response.StartedAt ?? DateTimeOffset.UtcNow;
                Provenance prov = ProvenanceMapper.ToFhirProvenance(obsFullUrl, obs.Provenance, occurredAt, response.DeviceId);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:prov-{obs.Code}-{obs.SubId ?? "0"}",
                    Resource = prov
                });
            }
        }

        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}

internal sealed record SessionsSummaryReport(int SessionCount, decimal AvgDurationMinutes, DateTimeOffset From, DateTimeOffset To);
