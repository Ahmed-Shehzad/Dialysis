using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Cds.Api.Controllers;

/// <summary>
/// Clinical decision support: prescription vs treatment compliance.
/// Returns FHIR DetectedIssue when treatment deviates from prescription.
/// </summary>
[ApiController]
[Route("api/cds")]
[Authorize(Policy = "CdsRead")]
public sealed class PrescriptionComplianceController : ControllerBase
{
    private readonly PrescriptionComplianceService _cds;
    private readonly IHttpClientFactory _http;

    public PrescriptionComplianceController(PrescriptionComplianceService cds, IHttpClientFactory http)
    {
        _cds = cds;
        _http = http;
    }

    /// <summary>
    /// Evaluates session against prescription. Returns DetectedIssue Bundle if deviation.
    /// </summary>
    [HttpGet("prescription-compliance")]
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
        using (HttpResponseMessage sessionResponse = await _http.CreateClient().SendAsync(sessionRequest, cancellationToken))
        {
            sessionResponse.EnsureSuccessStatusCode();
            TreatmentSessionResponse? session = await sessionResponse.Content.ReadFromJsonAsync<TreatmentSessionResponse>(cancellationToken);
            if (session is null)
                return NotFound();

            using var rxRequest = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/prescriptions/{session.PatientMrn}");
            if (Request.Headers.Authorization.Count > 0)
                rxRequest.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
            if (!string.IsNullOrEmpty(tenantId))
                rxRequest.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
            using (HttpResponseMessage rxResponse = await _http.CreateClient().SendAsync(rxRequest, cancellationToken))
            {
                PrescriptionDto? prescription = null;
                if (rxResponse.IsSuccessStatusCode)
                {
                    PrescriptionByMrnResponse? prescriptionResponse = await rxResponse.Content.ReadFromJsonAsync<PrescriptionByMrnResponse>(cancellationToken);
                    if (prescriptionResponse is not null)
                        prescription = new PrescriptionDto(prescriptionResponse.BloodFlowRateMlMin, prescriptionResponse.UfRateMlH, prescriptionResponse.UfTargetVolumeMl);
                }

                var observations = session.Observations.Select(o => new ObservationDto(o.Code, o.Value, o.Unit)).ToList();
                DetectedIssue? issue = _cds.Evaluate(sessionId, session.PatientMrn, observations, prescription);

                if (issue is null)
                {
                    var emptyBundle = new Hl7.Fhir.Model.Bundle { Type = Hl7.Fhir.Model.Bundle.BundleType.Collection, Entry = [] };
                    return Content(FhirJsonHelper.ToJson(emptyBundle), "application/fhir+json");
                }

                var bundle = new Hl7.Fhir.Model.Bundle
                {
                    Type = Hl7.Fhir.Model.Bundle.BundleType.Collection,
                    Entry = [new Hl7.Fhir.Model.Bundle.EntryComponent { Resource = issue }]
                };
                return Content(FhirJsonHelper.ToJson(bundle), "application/fhir+json");
            }
        }

    }
}

internal sealed record TreatmentSessionResponse(string SessionId, string? PatientMrn, IReadOnlyList<TreatmentObservationDto> Observations);
internal sealed record TreatmentObservationDto(string Code, string? Value, string? Unit);
internal sealed record PrescriptionByMrnResponse(decimal? BloodFlowRateMlMin, decimal? UfTargetVolumeMl, decimal? UfRateMlH);
