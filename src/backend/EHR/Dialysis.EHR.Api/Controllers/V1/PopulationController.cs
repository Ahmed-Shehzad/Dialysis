using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Population / cohort quality — runs the per-patient quality-measure evaluator across the active
/// panel and rolls the open care gaps up per measure, the basis for proactive outreach.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/population")]
public sealed class PopulationController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public PopulationController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Open quality gaps across the cohort, aggregated per measure plus a per-patient breakdown.</summary>
    [HttpGet("quality")]
    [ProducesResponseType(typeof(CohortQualityResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> QualityAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await _gateway.SendQueryAsync<EvaluateCohortQualityQuery, CohortQualityResult>(
            new EvaluateCohortQualityQuery(take), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Condition-control rate for a configured measure (e.g. % of hypertensives with BP controlled).</summary>
    [HttpGet("control")]
    [ProducesResponseType(typeof(PopulationControlResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ControlAsync(
        [FromQuery] string measureId, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await _gateway.SendQueryAsync<EvaluatePopulationControlQuery, PopulationControlResult>(
            new EvaluatePopulationControlQuery(measureId, take), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Reaches out to the patients whose condition is uncontrolled for a measure. Returns the audited
    /// target list; only dispatches real notifications when <c>Ehr:Population:Outreach:Enabled</c>.
    /// </summary>
    [HttpPost("outreach")]
    [ProducesResponseType(typeof(OutreachResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> OutreachAsync([FromBody] OutreachRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await _gateway.SendCommandAsync<NotifyAtRiskCohortCommand, OutreachResult>(
            new NotifyAtRiskCohortCommand(body.MeasureId, body.Take), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>At-risk outreach request body.</summary>
    public sealed record OutreachRequest(string MeasureId, int Take = 100);
}
