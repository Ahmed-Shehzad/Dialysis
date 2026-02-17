using Asp.Versioning;

using Dialysis.Gateway.Features.Fhir.PatientEverything;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

/// <summary>
/// FHIR bulk export - Patient $everything (Bundle with all resources for a patient).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Patient")]
public sealed class FhirExportController : ControllerBase
{
    private readonly ISender _sender;

    public FhirExportController(ISender sender) => _sender = sender;

    /// <summary>
    /// Patient $everything - returns Bundle with Patient, Conditions, EpisodesOfCare, Encounters, Observations, Procedures for the patient.
    /// FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}/everything")]
    [HttpGet("{id}/$everything")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> PatientEverything(string id, CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var query = new PatientEverythingQuery(baseUrl, id);
        var result = await _sender.SendAsync(query, cancellationToken);

        if (result is null)
            return NotFound();

        return Content(result.FhirBundleJson, "application/fhir+json");
    }
}
