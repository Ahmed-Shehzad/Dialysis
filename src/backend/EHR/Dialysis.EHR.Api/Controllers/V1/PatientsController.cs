using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;
using Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;
using Dialysis.EHR.PatientChart.Features.GetPatientChart;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.GetPatientById;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
public sealed class PatientsController : ControllerBase
{
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
}
