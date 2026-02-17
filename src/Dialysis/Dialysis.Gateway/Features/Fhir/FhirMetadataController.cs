using Asp.Versioning;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Fhir;

/// <summary>
/// FHIR metadata endpoint. Returns CapabilityStatement per FHIR R4 spec.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("fhir/r4")]
public sealed class FhirMetadataController : ControllerBase
{
    private readonly ISender _sender;

    public FhirMetadataController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// CapabilityStatement describing this server's FHIR R4 support. EHR clients use this for discovery.
    /// </summary>
    [HttpGet("metadata")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> Metadata(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var result = await _sender.SendAsync(new GetFhirMetadataQuery(baseUrl), ct);
        return Content(result.Json, result.ContentType);
    }
}
