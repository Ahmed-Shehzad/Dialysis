using Asp.Versioning;
using Dialysis.Gateway.Features.Fhir.Session;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Encounter")]
public sealed class FhirEncounterController : ControllerBase
{
    private readonly ISender _sender;

    public FhirEncounterController(ISender sender) => _sender = sender;

    /// <summary>
    /// Read an Encounter (dialysis visit) by ID. Id matches Session id. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken)
    {
        if (!Ulid.TryParse(id, out _))
            return NotFound();

        var session = await _sender.SendAsync(new GetSessionQuery(id), cancellationToken);
        if (session is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        return Content(FhirMappers.ToFhirJson(FhirMappers.ToFhirEncounter(session, baseUrl)), "application/fhir+json");
    }

    /// <summary>
    /// Search Encounters (dialysis visits) by patient. FHIR R4. Query param: patient={id}. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Search([FromQuery] string? patient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patient))
            return BadRequest(new { error = "patient query parameter is required." });

        var sessions = await _sender.SendAsync(new SearchSessionsQuery(patient), cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = global::Hl7.Fhir.Model.Bundle.BundleType.Searchset,
            Total = sessions.Count
        };
        foreach (var session in sessions)
        {
            var fhir = FhirMappers.ToFhirEncounter(session, baseUrl);
            bundle.Entry.Add(new global::Hl7.Fhir.Model.Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}Encounter/{session.Id}",
                Resource = fhir
            });
        }
        return Content(FhirMappers.ToFhirJson(bundle), "application/fhir+json");
    }
}
