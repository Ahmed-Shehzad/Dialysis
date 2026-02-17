using Asp.Versioning;

using Dialysis.Gateway.Features.Fhir;
using Dialysis.Gateway.Features.Meds;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/MedicationAdministration")]
public sealed class FhirMedicationAdministrationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMedicationAdministrationRepository _repository;
    private readonly ITenantContext _tenantContext;

    public FhirMedicationAdministrationController(
        ISender sender,
        IMedicationAdministrationRepository repository,
        ITenantContext tenantContext)
    {
        _sender = sender;
        _repository = repository;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Read MedicationAdministration by ID. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var med = await _repository.GetAsync(tenantId, id, cancellationToken);
        if (med is null)
            return NotFound();

        var baseUrl = GetBaseUrl();
        return Content(FhirMappers.ToFhirJson(FhirMappers.ToFhirMedicationAdministration(med, baseUrl)), "application/fhir+json");
    }

    /// <summary>
    /// Search MedicationAdministrations by patient. FHIR R4. Query: patient={id}. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? patient,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patient))
            return BadRequest(new { error = "patient query parameter is required." });

        var meds = await _sender.SendAsync(new ListMedicationsQuery(patient, Limit: 100), cancellationToken);
        var baseUrl = GetBaseUrl();
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = global::Hl7.Fhir.Model.Bundle.BundleType.Searchset,
            Total = meds.Count
        };
        foreach (var m in meds)
        {
            var fhir = FhirMappers.ToFhirMedicationAdministration(m, baseUrl);
            bundle.Entry.Add(new global::Hl7.Fhir.Model.Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}MedicationAdministration/{m.Id}",
                Resource = fhir
            });
        }
        return Content(FhirMappers.ToFhirJson(bundle), "application/fhir+json");
    }

    private string GetBaseUrl()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}/fhir/r4/";
    }
}
