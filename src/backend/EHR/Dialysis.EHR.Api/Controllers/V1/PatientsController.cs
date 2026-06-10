using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;
using Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;
using Dialysis.EHR.Integration.Features.IngestLabResult;
using Dialysis.EHR.PatientChart.Features.GetPatientChart;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.GetPatientById;
using Dialysis.EHR.Registration.Features.GetPatientsByIds;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
public sealed class PatientsController : ControllerBase
{
    // Caps the batch identity lookup so one request can't fan out unbounded; the SPA resolver chunks to
    // this size. Matches the SearchPatients take ceiling.
    private const int MaxBatchSize = 200;

    private readonly ICqrsGateway _gateway;
    public PatientsController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpGet]
    [ProducesResponseType(typeof(PatientSearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? q = null,
        [FromQuery] string? familyName = null,
        [FromQuery] string? givenName = null,
        [FromQuery] string? mrn = null,
        [FromQuery] DateOnly? dobFrom = null,
        [FromQuery] DateOnly? dobTo = null,
        [FromQuery] string? sex = null,
        [FromQuery] PatientStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var results = await _gateway
            .SendQueryAsync<SearchPatientsQuery, PatientSearchResult>(
                new SearchPatientsQuery(q, familyName, givenName, mrn, dobFrom, dobTo, sex, status, skip, take),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(results);
    }

    [HttpGet("{patientId:guid}")]
    [ProducesResponseType(typeof(PatientDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var detail = await _gateway
            .SendQueryAsync<GetPatientByIdQuery, PatientDetailDto?>(
                new GetPatientByIdQuery(patientId), cancellationToken)
            .ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>
    /// Batch identity lookup for label rendering — resolves many patient ids in ONE call so a list page
    /// with N rows avoids an N+1 of single fetches. Ids go in the body (not the query string) to keep
    /// patient identifiers out of gateway / proxy access logs. Returns a slim name+MRN+DOB projection;
    /// missing ids are simply absent. Same <c>PatientRead</c> gate as the single fetch.
    /// </summary>
    [HttpPost("by-ids")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientLabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByIdsAsync(
        [FromBody] PatientsByIdsRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var ids = (body.Ids ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return Ok(Array.Empty<PatientLabelDto>());
        if (ids.Length > MaxBatchSize)
            return BadRequest(new { error = $"At most {MaxBatchSize} ids per request." });

        var rows = await _gateway
            .SendQueryAsync<GetPatientsByIdsQuery, IReadOnlyList<PatientLabelDto>>(
                new GetPatientsByIdsQuery(ids), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("{patientId:guid}/chart")]
    [ProducesResponseType(typeof(PatientChartView), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChartAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var chart = await _gateway
            .SendQueryAsync<GetPatientChartQuery, PatientChartView>(
                new GetPatientChartQuery(patientId), cancellationToken)
            .ConfigureAwait(false);
        return Ok(chart);
    }

    /// <summary>
    /// Patient-scoped recent clinical notes. Backs the chart's Notes section so a
    /// clinician can see what's been written across encounters without drilling into
    /// each one. Ordered most-recent first.
    /// </summary>
    [HttpGet("{patientId:guid}/notes")]
    [ProducesResponseType(typeof(IReadOnlyList<ClinicalNoteListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotesAsync(
        Guid patientId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var notes = await _gateway
            .SendQueryAsync<ListNotesForPatientQuery, IReadOnlyList<ClinicalNoteListItem>>(
                new ListNotesForPatientQuery(patientId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(notes);
    }

    /// <summary>
    /// Patient-scoped lab results. Backs the patient-portal Lab results panel and any
    /// clinician view that needs a result feed. Default 180-day lookback, ordered
    /// most-recent first.
    /// </summary>
    [HttpGet("{patientId:guid}/lab-results")]
    [ProducesResponseType(typeof(IReadOnlyList<LabResultListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLabResultsAsync(
        Guid patientId,
        [FromQuery] int lookbackDays = 180,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await _gateway
            .SendQueryAsync<ListLabResultsForPatientQuery, IReadOnlyList<LabResultListItem>>(
                new ListLabResultsForPatientQuery(patientId, lookbackDays, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(results);
    }

    /// <summary>
    /// Ingests a lab observation result for a patient and persists it to the chart's lab-results read
    /// model. Normally driven by an inbound HL7v2 ORU / FHIR Observation (via SmartConnect → the Lab
    /// module); exposed here so a result can be recorded directly (e.g. the data simulator) closing
    /// the order → result loop the chart and the patient portal render.
    /// </summary>
    [HttpPost("{patientId:guid}/lab-results")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> IngestLabResultAsync(
        Guid patientId,
        [FromBody] IngestLabResultRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<IngestLabResultCommand, Guid>(
            new IngestLabResultCommand(
                LabFacilityCode: string.IsNullOrWhiteSpace(body.LabFacilityCode) ? "SIM-LAB" : body.LabFacilityCode,
                ExternalControlNumber: string.IsNullOrWhiteSpace(body.ExternalControlNumber)
                    ? Guid.NewGuid().ToString("N")
                    : body.ExternalControlNumber,
                LabOrderId: body.LabOrderId,
                PatientId: patientId,
                LoincCode: body.LoincCode,
                ValueText: body.ValueText,
                UnitCode: body.UnitCode,
                ReferenceRangeText: body.ReferenceRangeText,
                AbnormalFlagCode: string.IsNullOrWhiteSpace(body.AbnormalFlagCode) ? "N" : body.AbnormalFlagCode,
                ObservedAtUtc: body.ObservedAtUtc ?? DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/patients/{patientId}/lab-results/{id}", new { id });
    }

    /// <summary>Batch identity-lookup body — the patient ids to resolve to labels (capped per request).</summary>
    public sealed record PatientsByIdsRequest(IReadOnlyList<Guid> Ids);

    /// <summary>Inbound lab-result body (the lab observation to record against an order).</summary>
    public sealed record IngestLabResultRequest(
        Guid LabOrderId,
        string LoincCode,
        string ValueText,
        string? UnitCode = null,
        string? ReferenceRangeText = null,
        string? AbnormalFlagCode = null,
        DateTime? ObservedAtUtc = null,
        string? LabFacilityCode = null,
        string? ExternalControlNumber = null);
}
