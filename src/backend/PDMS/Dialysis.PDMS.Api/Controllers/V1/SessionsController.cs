using Asp.Versioning;
using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Features.AbortSession;
using Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;
using Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessions;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;
using Dialysis.PDMS.TreatmentSessions.Features.PauseSession;
using Dialysis.PDMS.TreatmentSessions.Features.RecordAdverseEvent;
using Dialysis.PDMS.TreatmentSessions.Features.RecordReading;
using Dialysis.PDMS.TreatmentSessions.Features.ResumeSession;
using Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;
using Dialysis.PDMS.TreatmentSessions.Features.StartSession;
using Dialysis.PDMS.TreatmentSessions.Realtime;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public SessionsController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DialysisSessionListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var data = await _gateway
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
        var data = await _gateway
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
            var summary = await _gateway
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
        var readings = await _gateway
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
        var id = await _gateway
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
        await _gateway
            .SendCommandAsync<StartSessionCommand, Unit>(new StartSessionCommand(sessionId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PauseAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _gateway
            .SendCommandAsync<PauseSessionCommand, Unit>(new PauseSessionCommand(sessionId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResumeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _gateway
            .SendCommandAsync<ResumeSessionCommand, Unit>(new ResumeSessionCommand(sessionId), cancellationToken)
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
        await _gateway
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
        await _gateway
            .SendCommandAsync<AbortSessionCommand, Unit>(
                new AbortSessionCommand(sessionId, body.ReasonCode), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Records an intradialytic adverse event observed on the session. Fans out to the EHR
    /// safety-surveillance read model via an integration event.
    /// </summary>
    [HttpPost("{sessionId:guid}/adverse-events")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RecordAdverseEventAsync(
        Guid sessionId,
        [FromBody] RecordAdverseEventRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _gateway
            .SendCommandAsync<RecordAdverseEventCommand, Unit>(
                new RecordAdverseEventCommand(sessionId, body.EventKindCode, body.Severity, body.Notes), cancellationToken)
            .ConfigureAwait(false);
        return Accepted();
    }

    [HttpPost("{sessionId:guid}/readings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RecordReadingAsync(
        Guid sessionId,
        [FromBody] RecordReadingRequest body,
        [FromServices] IConfiguration configuration,
        [FromServices] IDurableCommandBus durableCommandBus,
        [FromHeader(Name = "X-Command-Id")] Guid? commandId,
        CancellationToken cancellationToken)
    {
        // Feature flag — false (default) keeps the existing synchronous path; true routes the
        // write through the durable command bus and returns 202 with a poll URL. The flag
        // exists so PR-A can ship the mechanism without flipping production traffic; ops can
        // promote per environment once they're happy with the consumer + status surface.
        var useDurablePath = configuration.GetValue("Pdms:DurableCommands:RecordReading:Enabled", false);
        var readingId = commandId ?? Guid.CreateVersion7();
        var command = new RecordReadingCommand(
            sessionId,
            body.SystolicBloodPressure,
            body.DiastolicBloodPressure,
            body.HeartRateBpm,
            body.ArterialPressureMmHg,
            body.VenousPressureMmHg,
            body.UltrafiltrationRateMlPerHour,
            body.ConductivityMsPerCm,
            body.Notes,
            ReadingId: readingId);

        if (useDurablePath)
        {
            try
            {
                var acceptance = await durableCommandBus
                    .EnqueueAsync<RecordReadingCommand, Guid>(command, commandId: readingId, cancellationToken)
                    .ConfigureAwait(false);
                Response.Headers["Location"] = acceptance.StatusEndpoint;
                return Accepted(acceptance.StatusEndpoint, new
                {
                    commandId = acceptance.CommandId,
                    correlationId = acceptance.CorrelationId,
                    statusEndpoint = acceptance.StatusEndpoint,
                    // The id is deterministic from CommandId (see DialysisSession.RecordReading),
                    // so the caller knows the new reading row's id without polling.
                    readingId,
                });
            }
            catch (DurableCommandException)
            {
                Response.Headers.Append("Retry-After", "5");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        var id = await _gateway
            .SendCommandAsync<RecordReadingCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return Created($"/api/v1.0/sessions/{sessionId}/readings/{id}", new { id });
    }

    public sealed record ScheduleSessionRequest
    {
        public ScheduleSessionRequest(Guid PatientId,
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
            DateOnly AccessEstablishedOn)
        {
            this.PatientId = PatientId;
            this.ScheduledStartUtc = ScheduledStartUtc;
            this.DialyzerModel = DialyzerModel;
            this.PrescribedDurationMinutes = PrescribedDurationMinutes;
            this.BloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
            this.DialysateFlowRateMlPerMin = DialysateFlowRateMlPerMin;
            this.DialysatePotassiumMmolPerL = DialysatePotassiumMmolPerL;
            this.DialysateCalciumMmolPerL = DialysateCalciumMmolPerL;
            this.DialysateSodiumMmolPerL = DialysateSodiumMmolPerL;
            this.TargetUfVolumeLiters = TargetUfVolumeLiters;
            this.AnticoagulationProtocolCode = AnticoagulationProtocolCode;
            this.AccessKind = AccessKind;
            this.AccessSite = AccessSite;
            this.AccessEstablishedOn = AccessEstablishedOn;
        }
        public Guid PatientId { get; init; }
        public DateTime ScheduledStartUtc { get; init; }
        public string DialyzerModel { get; init; }
        public int PrescribedDurationMinutes { get; init; }
        public int BloodFlowRateMlPerMin { get; init; }
        public int DialysateFlowRateMlPerMin { get; init; }
        public decimal DialysatePotassiumMmolPerL { get; init; }
        public decimal DialysateCalciumMmolPerL { get; init; }
        public decimal DialysateSodiumMmolPerL { get; init; }
        public decimal TargetUfVolumeLiters { get; init; }
        public string AnticoagulationProtocolCode { get; init; }
        public VascularAccessKind AccessKind { get; init; }
        public string AccessSite { get; init; }
        public DateOnly AccessEstablishedOn { get; init; }
        public void Deconstruct(out Guid patientId, out DateTime scheduledStartUtc, out string dialyzerModel, out int prescribedDurationMinutes, out int bloodFlowRateMlPerMin, out int dialysateFlowRateMlPerMin, out decimal dialysatePotassiumMmolPerL, out decimal dialysateCalciumMmolPerL, out decimal dialysateSodiumMmolPerL, out decimal targetUfVolumeLiters, out string anticoagulationProtocolCode, out VascularAccessKind accessKind, out string accessSite, out DateOnly accessEstablishedOn)
        {
            patientId = this.PatientId;
            scheduledStartUtc = this.ScheduledStartUtc;
            dialyzerModel = this.DialyzerModel;
            prescribedDurationMinutes = this.PrescribedDurationMinutes;
            bloodFlowRateMlPerMin = this.BloodFlowRateMlPerMin;
            dialysateFlowRateMlPerMin = this.DialysateFlowRateMlPerMin;
            dialysatePotassiumMmolPerL = this.DialysatePotassiumMmolPerL;
            dialysateCalciumMmolPerL = this.DialysateCalciumMmolPerL;
            dialysateSodiumMmolPerL = this.DialysateSodiumMmolPerL;
            targetUfVolumeLiters = this.TargetUfVolumeLiters;
            anticoagulationProtocolCode = this.AnticoagulationProtocolCode;
            accessKind = this.AccessKind;
            accessSite = this.AccessSite;
            accessEstablishedOn = this.AccessEstablishedOn;
        }
    }

    public sealed record CompleteSessionRequest
    {
        public CompleteSessionRequest(decimal AchievedUfVolumeLiters) => this.AchievedUfVolumeLiters = AchievedUfVolumeLiters;
        public decimal AchievedUfVolumeLiters { get; init; }
        public void Deconstruct(out decimal achievedUfVolumeLiters) => achievedUfVolumeLiters = this.AchievedUfVolumeLiters;
    }

    public sealed record AbortSessionRequest
    {
        public AbortSessionRequest(string ReasonCode) => this.ReasonCode = ReasonCode;
        public string ReasonCode { get; init; }
        public void Deconstruct(out string reasonCode) => reasonCode = this.ReasonCode;
    }

    public sealed record RecordAdverseEventRequest
    {
        public RecordAdverseEventRequest(string EventKindCode, string Severity, string? Notes)
        {
            this.EventKindCode = EventKindCode;
            this.Severity = Severity;
            this.Notes = Notes;
        }
        public string EventKindCode { get; init; }
        public string Severity { get; init; }
        public string? Notes { get; init; }
        public void Deconstruct(out string eventKindCode, out string severity, out string? notes)
        {
            eventKindCode = this.EventKindCode;
            severity = this.Severity;
            notes = this.Notes;
        }
    }

    public sealed record RecordReadingRequest
    {
        public RecordReadingRequest(int SystolicBloodPressure,
            int DiastolicBloodPressure,
            int HeartRateBpm,
            decimal ArterialPressureMmHg,
            decimal VenousPressureMmHg,
            decimal UltrafiltrationRateMlPerHour,
            decimal ConductivityMsPerCm,
            string? Notes)
        {
            this.SystolicBloodPressure = SystolicBloodPressure;
            this.DiastolicBloodPressure = DiastolicBloodPressure;
            this.HeartRateBpm = HeartRateBpm;
            this.ArterialPressureMmHg = ArterialPressureMmHg;
            this.VenousPressureMmHg = VenousPressureMmHg;
            this.UltrafiltrationRateMlPerHour = UltrafiltrationRateMlPerHour;
            this.ConductivityMsPerCm = ConductivityMsPerCm;
            this.Notes = Notes;
        }
        public int SystolicBloodPressure { get; init; }
        public int DiastolicBloodPressure { get; init; }
        public int HeartRateBpm { get; init; }
        public decimal ArterialPressureMmHg { get; init; }
        public decimal VenousPressureMmHg { get; init; }
        public decimal UltrafiltrationRateMlPerHour { get; init; }
        public decimal ConductivityMsPerCm { get; init; }
        public string? Notes { get; init; }
        public void Deconstruct(out int systolicBloodPressure, out int diastolicBloodPressure, out int heartRateBpm, out decimal arterialPressureMmHg, out decimal venousPressureMmHg, out decimal ultrafiltrationRateMlPerHour, out decimal conductivityMsPerCm, out string? notes)
        {
            systolicBloodPressure = this.SystolicBloodPressure;
            diastolicBloodPressure = this.DiastolicBloodPressure;
            heartRateBpm = this.HeartRateBpm;
            arterialPressureMmHg = this.ArterialPressureMmHg;
            venousPressureMmHg = this.VenousPressureMmHg;
            ultrafiltrationRateMlPerHour = this.UltrafiltrationRateMlPerHour;
            conductivityMsPerCm = this.ConductivityMsPerCm;
            notes = this.Notes;
        }
    }
}
