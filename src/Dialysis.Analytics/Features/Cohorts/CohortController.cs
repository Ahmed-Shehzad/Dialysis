using Asp.Versioning;
using Dialysis.Analytics.Data;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Analytics.Features.Cohorts;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/cohorts")]
[Authorize(Policy = "Read")]
public sealed class CohortController : ControllerBase
{
    private readonly ISender _sender;

    public CohortController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>List saved cohort definitions.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var cohorts = await _sender.SendAsync(new ListCohortsQuery(), cancellationToken);
        return Ok(cohorts.Select(c => new { c.Id, c.Name, c.Criteria, c.CreatedAt, c.UpdatedAt }));
    }

    /// <summary>Get saved cohort by ID.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var cohort = await _sender.SendAsync(new GetCohortQuery(id), cancellationToken);
        return cohort != null ? Ok(cohort) : NotFound();
    }

    /// <summary>Create or update saved cohort definition.</summary>
    [HttpPost]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Save([FromBody] SaveCohortRequest request, CancellationToken cancellationToken)
    {
        var saved = await _sender.SendAsync(
            new SaveCohortCommand(request.Id, request.Name, request.Criteria),
            cancellationToken);
        return Ok(saved);
    }

    /// <summary>Delete saved cohort.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _sender.SendAsync(new DeleteCohortCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Resolve cohort members by criteria. Returns patient and encounter IDs.</summary>
    [HttpPost("resolve")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Resolve([FromBody] CohortCriteria criteria, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new ResolveCohortQuery(criteria), cancellationToken);
        return Ok(new
        {
            patientIds = result.PatientIds,
            encounterIds = result.EncounterIds,
            totalPatients = result.TotalPatients,
            totalEncounters = result.TotalEncounters
        });
    }

    /// <summary>Resolve saved cohort by ID. Returns patient and encounter IDs.</summary>
    [HttpPost("{id}/resolve")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> ResolveSaved(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new ResolveSavedCohortQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(new
        {
            cohortId = result.CohortId,
            cohortName = result.CohortName,
            patientIds = result.PatientIds,
            encounterIds = result.EncounterIds,
            totalPatients = result.TotalPatients,
            totalEncounters = result.TotalEncounters
        });
    }
}

public sealed record SaveCohortRequest(string? Id, string? Name, CohortCriteria? Criteria);
