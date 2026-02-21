using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Mvc;

using Refit;

namespace Dialysis.Cds.Api.Controllers;

/// <summary>
/// Clinical decision support: prescription vs treatment compliance.
/// Returns FHIR DetectedIssue when treatment deviates from prescription.
/// </summary>
[ApiController]
[Route("api/cds")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "CdsRead")]
public sealed class PrescriptionComplianceController : ControllerBase
{
    private readonly PrescriptionComplianceService _cds;
    private readonly ICdsGatewayApi _api;
    private readonly ITenantContext _tenant;

    public PrescriptionComplianceController(PrescriptionComplianceService cds, ICdsGatewayApi api, ITenantContext tenant)
    {
        _cds = cds;
        _api = api;
        _tenant = tenant;
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
        string? auth = Request.Headers.Authorization.Count > 0 ? Request.Headers.Authorization.ToString() : null;
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;

        TreatmentSessionResponse session;
        try
        {
            session = await _api.GetTreatmentSessionAsync(sessionId, auth, tenantId, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        PrescriptionDto? prescription = null;
        if (!string.IsNullOrEmpty(session.PatientMrn))
        {
            IApiResponse<PrescriptionByMrnResponse> rxResponse = await _api.GetPrescriptionByMrnAsync(session.PatientMrn, auth, tenantId, cancellationToken);
            if (rxResponse.IsSuccessStatusCode && rxResponse.Content is { } prescriptionResponse)
                prescription = new PrescriptionDto(prescriptionResponse.BloodFlowRateMlMin, prescriptionResponse.UfRateMlH, prescriptionResponse.UfTargetVolumeMl);
        }

        var observations = session.Observations.Select(o => new ObservationDto(o.Code, o.Value, o.Unit)).ToList();
        DetectedIssue? issue = _cds.Evaluate(sessionId, session.PatientMrn, observations, prescription);

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

