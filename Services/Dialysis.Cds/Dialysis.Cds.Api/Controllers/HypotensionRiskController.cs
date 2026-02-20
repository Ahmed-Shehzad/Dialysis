using BuildingBlocks.Tenancy;

using Dialysis.Cds.Api;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Cds.Api.Controllers;

/// <summary>
/// CDS: hypotension risk (systolic &lt; 90 or diastolic &lt; 60 mmHg).
/// Returns FHIR DetectedIssue Bundle when BP below threshold.
/// </summary>
[ApiController]
[Route("api/cds")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "CdsRead")]
public sealed class HypotensionRiskController : ControllerBase
{
    private readonly HypotensionRiskService _hypotension;
    private readonly ICdsGatewayApi _api;
    private readonly ITenantContext _tenant;

    public HypotensionRiskController(HypotensionRiskService hypotension, ICdsGatewayApi api, ITenantContext tenant)
    {
        _hypotension = hypotension;
        _api = api;
        _tenant = tenant;
    }

    /// <summary>
    /// Evaluates session for hypotension risk. Returns DetectedIssue Bundle if BP below threshold.
    /// </summary>
    [HttpGet("hypotension-risk")]
    [Produces("application/fhir+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EvaluateAsync(
        [FromQuery] string sessionId,
        CancellationToken cancellationToken = default)
    {
        string? auth = Request.Headers.Authorization.Count > 0 ? Request.Headers.Authorization.ToString() : null;
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;

        TreatmentSessionResponse session;
        try
        {
            session = await _api.GetTreatmentSessionAsync(sessionId, auth, tenantId, cancellationToken);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var observations = session.Observations.Select(o => new ObservationDto(o.Code, o.Value, o.Unit)).ToList();
        DetectedIssue? issue = _hypotension.Evaluate(sessionId, session.PatientMrn, observations);

        if (issue is null)
        {
            var emptyBundle = new Bundle { Type = Bundle.BundleType.Collection, Entry = [] };
            return Content(FhirJsonHelper.ToJson(emptyBundle), "application/fhir+json");
        }

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = [new Bundle.EntryComponent { Resource = issue }]
        };
        return Content(FhirJsonHelper.ToJson(bundle), "application/fhir+json");
    }
}
