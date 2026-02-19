using BuildingBlocks.Tenancy;

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
[Authorize(Policy = "CdsRead")]
public sealed class VenousPressureRiskController : ControllerBase
{
    private readonly VenousPressureRiskService _service;
    private readonly IHttpClientFactory _http;

    public VenousPressureRiskController(VenousPressureRiskService service, IHttpClientFactory http)
    {
        _service = service;
        _http = http;
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
        string baseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Cds:BaseUrl"] ?? "http://localhost:5000";
        string tenantId = HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;
        using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/treatment-sessions/{sessionId}");
        if (Request.Headers.Authorization.Count > 0)
            req.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
        if (!string.IsNullOrEmpty(tenantId))
            req.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        using var response = await _http.CreateClient().SendAsync(req, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TreatmentSessionResponse>(ct);
    }

    private IActionResult ToFhirBundle(DetectedIssue? issue)
    {
        if (issue is null)
            return Content(FhirJsonHelper.ToJson(new Bundle { Type = Bundle.BundleType.Collection, Entry = [] }), "application/fhir+json");
        return Content(FhirJsonHelper.ToJson(new Bundle { Type = Bundle.BundleType.Collection, Entry = [new Bundle.EntryComponent { Resource = issue }] }), "application/fhir+json");
    }
}
