using Asp.Versioning;
using Dialysis.DeviceIngestion.Features.Observations.Get;
using Dialysis.DeviceIngestion.Features.Observations.Search;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Observation")]
public sealed class FhirObservationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITenantContext _tenantContext;

    public FhirObservationController(ISender sender, ITenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Read a single Observation by ID. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var observationId = new ObservationId(id);

        var query = new GetObservationQuery(tenantId, observationId);
        var observation = await _sender.SendAsync(query, cancellationToken);

        if (observation is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var fhir = FhirMappers.ToFhirObservation(observation, baseUrl);
        var json = FhirMappers.ToFhirJson(fhir);

        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// Search Observations by patient. FHIR R4. Query param: patient={id}. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Search([FromQuery] string? patient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patient))
            return BadRequest(new { error = "patient query parameter is required." });

        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(patient);

        var query = new SearchObservationsQuery(tenantId, patientId);
        var observations = await _sender.SendAsync(query, cancellationToken);

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var fhirObservations = observations.Select(o => FhirMappers.ToFhirObservation(o, baseUrl)).ToList();
        var bundle = FhirMappers.ToFhirSearchBundle(fhirObservations, baseUrl);
        var json = FhirMappers.ToFhirJson(bundle);

        return Content(json, "application/fhir+json");
    }
}
