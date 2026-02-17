using Asp.Versioning;
using Dialysis.Gateway.Features.Fhir.Session;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Procedure")]
public sealed class FhirProcedureController : ControllerBase
{
    private readonly ISender _sender;

    public FhirProcedureController(ISender sender) => _sender = sender;

    /// <summary>
    /// Read a Procedure (dialysis session) by ID. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken)
    {
        var session = await _sender.SendAsync(new GetSessionQuery(id), cancellationToken);
        if (session is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        return Content(FhirMappers.ToFhirJson(FhirMappers.ToFhirProcedure(session, baseUrl)), "application/fhir+json");
    }

    /// <summary>
    /// Search Procedures (dialysis sessions) by patient. FHIR R4. Query param: patient={id}. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Search([FromQuery] string? patient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patient))
            return BadRequest(new { error = "patient query parameter is required." });

        var sessions = await _sender.SendAsync(new SearchSessionsQuery(patient), cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var bundle = FhirMappers.ToFhirProcedureSearchBundle(
            sessions.Select(s => FhirMappers.ToFhirProcedure(s, baseUrl)).ToList(),
            baseUrl);
        return Content(FhirMappers.ToFhirJson(bundle), "application/fhir+json");
    }
}
