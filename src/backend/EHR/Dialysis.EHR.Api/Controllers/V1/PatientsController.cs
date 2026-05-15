using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.PatientChart.Features.GetPatientChart;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
public sealed class PatientsController(ICqrsGateway gateway) : ControllerBase
{
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
        var results = await gateway
            .SendQueryAsync<SearchPatientsQuery, PatientSearchResult>(
                new SearchPatientsQuery(q, familyName, givenName, mrn, dobFrom, dobTo, sex, status, skip, take),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(results);
    }

    [HttpGet("{patientId:guid}/chart")]
    [ProducesResponseType(typeof(PatientChartView), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChartAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var chart = await gateway
            .SendQueryAsync<GetPatientChartQuery, PatientChartView>(
                new GetPatientChartQuery(patientId), cancellationToken)
            .ConfigureAwait(false);
        return Ok(chart);
    }
}
