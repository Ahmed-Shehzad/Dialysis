using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

/// <summary>
/// Base for module-specific FHIR controllers that prefer MVC over the dynamic
/// <see cref="FhirEndpointExtensions.MapFhirEndpoints"/> routing. Returns raw FHIR resources
/// — never wraps in the HATEOAS <c>ResourceEnvelope&lt;T&gt;</c>.
/// </summary>
[Produces(FhirContentTypes.Json)]
public abstract class FhirControllerBase : ControllerBase
{
    protected IActionResult FhirOk(Resource resource)
    {
        var serializer = HttpContext.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        return new FhirContentResult(resource, StatusCodes.Status200OK, serializer);
    }

    protected IActionResult FhirBundle(Bundle bundle)
    {
        var serializer = HttpContext.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        return new FhirContentResult(bundle, StatusCodes.Status200OK, serializer);
    }

    protected IActionResult FhirNotFound(string resourceType, string id)
    {
        var serializer = HttpContext.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        return new FhirContentResult(
            OperationOutcomeFactory.NotFound(resourceType, id),
            StatusCodes.Status404NotFound,
            serializer);
    }

    protected IActionResult FhirOperationOutcome(OperationOutcome outcome, int statusCode)
    {
        var serializer = HttpContext.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        return new FhirContentResult(outcome, statusCode, serializer);
    }
}
