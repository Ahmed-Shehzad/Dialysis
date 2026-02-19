using BuildingBlocks.Tenancy;

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
[Authorize(Policy = "CdsRead")]
public sealed class HypotensionRiskController : ControllerBase
{
    private readonly HypotensionRiskService _hypotension;
    private readonly IHttpClientFactory _http;

    public HypotensionRiskController(HypotensionRiskService hypotension, IHttpClientFactory http)
    {
        _hypotension = hypotension;
        _http = http;
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
        string baseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Cds:BaseUrl"] ?? "http://localhost:5000";
        string tenantId = HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;
        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/treatment-sessions/{sessionId}");
        if (Request.Headers.Authorization.Count > 0)
            sessionRequest.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
        if (!string.IsNullOrEmpty(tenantId))
            sessionRequest.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        using HttpResponseMessage sessionResponse = await _http.CreateClient().SendAsync(sessionRequest, cancellationToken);
        sessionResponse.EnsureSuccessStatusCode();
        TreatmentSessionResponse? session = await sessionResponse.Content.ReadFromJsonAsync<TreatmentSessionResponse>(cancellationToken);
        if (session is null)
            return NotFound();

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
