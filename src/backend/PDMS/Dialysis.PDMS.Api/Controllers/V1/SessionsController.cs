using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Features.AbortSession;
using Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;
using Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessions;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;
using Dialysis.PDMS.TreatmentSessions.Features.RecordReading;
using Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;
using Dialysis.PDMS.TreatmentSessions.Features.StartSession;
using Dialysis.PDMS.TreatmentSessions.Realtime;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
public sealed class SessionsController(ICqrsGateway gateway) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DialysisSessionListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var data = await gateway
            .SendQueryAsync<ListSessionsQuery, IReadOnlyList<DialysisSessionListItem>>(
                new ListSessionsQuery(activeOnly, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(data);
    }

    /// <summary>
    /// Patient-scoped recent treatments. Used by the patient portal "Recent treatments"
    /// panel and by any clinician view that needs one patient's session history. Ordered
    /// most-recent first.
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<DialysisSessionListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByPatientAsync(
        Guid patientId,
        [FromQuery] int lookbackDays = 90,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await gateway
            .SendQueryAsync<ListSessionsByPatientQuery, IReadOnlyList<DialysisSessionListItem>>(
                new ListSessionsByPatientQuery(patientId, lookbackDays, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(data);
    }

    [HttpGet("{sessionId:guid}/summary")]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummaryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await gateway
                .SendQueryAsync<GetSessionSummaryQuery, SessionSummaryDto>(
                    new GetSessionSummaryQuery(sessionId), cancellationToken)
                .ConfigureAwait(false);
            return Ok(summary);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{sessionId:guid}/readings")]
    [ProducesResponseType(typeof(IReadOnlyList<VitalsReadingSnapshot>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReadingsAsync(
        Guid sessionId,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var readings = await gateway
            .SendQueryAsync<ListSessionReadingsQuery, IReadOnlyList<VitalsReadingSnapshot>>(
                new ListSessionReadingsQuery(sessionId, limit), cancellationToken)
            .ConfigureAwait(false);
        return Ok(readings);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> ScheduleAsync(
        [FromBody] ScheduleSessionRequest body,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<ScheduleSessionCommand, Guid>(
                new ScheduleSessionCommand(
                    body.PatientId,
                    body.ScheduledStartUtc,
                    body.DialyzerModel,
                    body.PrescribedDurationMinutes,
                    body.BloodFlowRateMlPerMin,
                    body.DialysateFlowRateMlPerMin,
                    body.DialysatePotassiumMmolPerL,
                    body.DialysateCalciumMmolPerL,
                    body.DialysateSodiumMmolPerL,
                    body.TargetUfVolumeLiters,
                    body.AnticoagulationProtocolCode,
                    body.AccessKind,
                    body.AccessSite,
                    body.AccessEstablishedOn),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/api/v1.0/sessions/{id}", new { id });
    }

    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<StartSessionCommand, Unit>(new StartSessionCommand(sessionId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteAsync(
        Guid sessionId,
        [FromBody] CompleteSessionRequest body,
        CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<CompleteSessionCommand, Unit>(
                new CompleteSessionCommand(sessionId, body.AchievedUfVolumeLiters), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/abort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AbortAsync(
        Guid sessionId,
        [FromBody] AbortSessionRequest body,
        CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<AbortSessionCommand, Unit>(
                new AbortSessionCommand(sessionId, body.ReasonCode), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/readings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordReadingAsync(
        Guid sessionId,
        [FromBody] RecordReadingRequest body,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RecordReadingCommand, Guid>(
                new RecordReadingCommand(
                    sessionId,
                    body.SystolicBloodPressure,
                    body.DiastolicBloodPressure,
                    body.HeartRateBpm,
                    body.ArterialPressureMmHg,
                    body.VenousPressureMmHg,
                    body.UltrafiltrationRateMlPerHour,
                    body.ConductivityMsPerCm,
                    body.Notes),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/api/v1.0/sessions/{sessionId}/readings/{id}", new { id });
    }

    public sealed record ScheduleSessionRequest(
        Guid PatientId,
        DateTime ScheduledStartUtc,
        string DialyzerModel,
        int PrescribedDurationMinutes,
        int BloodFlowRateMlPerMin,
        int DialysateFlowRateMlPerMin,
        decimal DialysatePotassiumMmolPerL,
        decimal DialysateCalciumMmolPerL,
        decimal DialysateSodiumMmolPerL,
        decimal TargetUfVolumeLiters,
        string AnticoagulationProtocolCode,
        VascularAccessKind AccessKind,
        string AccessSite,
        DateOnly AccessEstablishedOn);

    public sealed record CompleteSessionRequest(decimal AchievedUfVolumeLiters);

    public sealed record AbortSessionRequest(string ReasonCode);

    public sealed record RecordReadingRequest(
        int SystolicBloodPressure,
        int DiastolicBloodPressure,
        int HeartRateBpm,
        decimal ArterialPressureMmHg,
        decimal VenousPressureMmHg,
        decimal UltrafiltrationRateMlPerHour,
        decimal ConductivityMsPerCm,
        string? Notes);
}
