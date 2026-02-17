using Asp.Versioning;
using Dialysis.DeviceIngestion.Features.Patients.Create;
using Dialysis.DeviceIngestion.Features.Patients.Get;
using Dialysis.Gateway.Features.Fhir;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Patient")]
public sealed class FhirPatientController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITenantContext _tenantContext;

    public FhirPatientController(ISender sender, ITenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Create a Patient from FHIR resource. Returns 409 if identifier already exists. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return BadRequest(new { error = "Request body is required." });

        try
        {
            var (logicalId, familyName, givenNames, birthDate) = FhirMappers.FromFhirPatientJson(json);
            var tenantId = _tenantContext.TenantId;
            var patientId = new PatientId(logicalId);

            var command = new CreatePatientCommand(tenantId, patientId, familyName, givenNames, birthDate);
            var result = await _sender.SendAsync(command, cancellationToken);

            var patient = await _sender.SendAsync(new GetPatientQuery(tenantId, result.LogicalId), cancellationToken);
            if (patient is null)
                return StatusCode(500, new { error = "Patient created but could not be retrieved." });

            var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
            var fhir = FhirMappers.ToFhirPatient(patient, baseUrl);
            var responseJson = FhirMappers.ToFhirJson(fhir);

            Response.Headers["Location"] = $"{baseUrl}Patient/{logicalId}";
            return new ContentResult
            {
                StatusCode = 201,
                Content = responseJson,
                ContentType = "application/fhir+json"
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Read a Patient resource by logical ID. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(id);

        var query = new GetPatientQuery(tenantId, patientId);
        var patient = await _sender.SendAsync(query, cancellationToken);

        if (patient is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var fhir = FhirMappers.ToFhirPatient(patient, baseUrl);
        var json = FhirMappers.ToFhirJson(fhir);

        return Content(json, "application/fhir+json");
    }
}
