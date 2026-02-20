using BuildingBlocks.Tenancy;

using Dialysis.Cds.Api;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Cds.Api.Controllers;

/// <summary>
/// CDS: high venous pressure risk (&gt; 200 mmHg).
/// </summary>
[ApiController]
[Route("api/cds")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "CdsRead")]
public sealed class VenousPressureRiskController : ControllerBase
{
    private readonly VenousPressureRiskService _service;
    private readonly ICdsGatewayApi _api;
    private readonly ITenantContext _tenant;

    public VenousPressureRiskController(VenousPressureRiskService service, ICdsGatewayApi api, ITenantContext tenant)
    {
        _service = service;
        _api = api;
        _tenant = tenant;
    }

    [HttpGet("venous-pressure-risk")]
    [Produces("application/fhir+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EvaluateAsync([FromQuery] string sessionId, CancellationToken cancellationToken = default)
    {
        TreatmentSessionResponse? session = await FetchSessionAsync(sessionId, cancellationToken);
        if (session is null)
            return NotFound();

        var observations = session.Observations.Select(o => new ObservationDto(o.Code, o.Value, o.Unit)).ToList();
        DetectedIssue? issue = _service.Evaluate(sessionId, session.PatientMrn, observations);

        return ToFhirBundle(issue);
    }

    private async Task<TreatmentSessionResponse?> FetchSessionAsync(string sessionId, CancellationToken ct)
    {
        string? auth = Request.Headers.Authorization.Count > 0 ? Request.Headers.Authorization.ToString() : null;
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;
        try
        {
            return await _api.GetTreatmentSessionAsync(sessionId, auth, tenantId, ct);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private IActionResult ToFhirBundle(DetectedIssue? issue)
    {
        if (issue is null)
            return Content(FhirJsonHelper.ToJson(new Bundle { Type = Bundle.BundleType.Collection, Entry = [] }), "application/fhir+json");
        return Content(FhirJsonHelper.ToJson(new Bundle { Type = Bundle.BundleType.Collection, Entry = [new Bundle.EntryComponent { Resource = issue }] }), "application/fhir+json");
    }
}
