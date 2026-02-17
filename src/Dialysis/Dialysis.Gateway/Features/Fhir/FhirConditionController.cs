using Asp.Versioning;
using Dialysis.Gateway.Features.Fhir.Condition;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4/Condition")]
public sealed class FhirConditionController : ControllerBase
{
    private readonly ISender _sender;

    public FhirConditionController(ISender sender) => _sender = sender;

    /// <summary>
    /// Read a Condition by ID. FHIR R4. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken)
    {
        var condition = await _sender.SendAsync(new GetConditionQuery(id), cancellationToken);
        if (condition is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        return Content(FhirMappers.ToFhirJson(FhirMappers.ToFhirCondition(condition, baseUrl)), "application/fhir+json");
    }

    /// <summary>
    /// Search Conditions by patient. FHIR R4. Query param: patient={id}. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Search([FromQuery] string? patient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patient))
            return BadRequest(new { error = "patient query parameter is required." });

        var conditions = await _sender.SendAsync(new SearchConditionsQuery(patient), cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = global::Hl7.Fhir.Model.Bundle.BundleType.Searchset,
            Total = conditions.Count
        };
        foreach (var c in conditions)
        {
            bundle.Entry.Add(new global::Hl7.Fhir.Model.Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}Condition/{c.Id}",
                Resource = FhirMappers.ToFhirCondition(c, baseUrl)
            });
        }
        return Content(FhirMappers.ToFhirJson(bundle), "application/fhir+json");
    }

    /// <summary>
    /// Create a Condition from FHIR resource. FHIR R4. Include X-Tenant-Id header.
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

        var result = await _sender.SendAsync(new CreateConditionCommand(json), cancellationToken);
        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var created = FhirMappers.ToFhirCondition(result.Condition!, baseUrl);
        Response.Headers.Append("Location", $"{baseUrl}Condition/{result.Condition!.Id}");
        return new ContentResult
        {
            StatusCode = 201,
            Content = FhirMappers.ToFhirJson(created),
            ContentType = "application/fhir+json"
        };
    }
}
