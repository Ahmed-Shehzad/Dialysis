using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

/// <summary>
/// Base for module-specific FHIR controllers that prefer MVC over the dynamic
/// <see cref="FhirEndpointExtensions.MapFhirEndpoints"/> routing. Returns raw FHIR resources
/// — never wraps in the HATEOAS <c>ResourceEnvelope&lt;T&gt;</c>.
/// </summary>
[Produces(FhirContentTypes.Json)]
public abstract class FhirControllerBase : ControllerBase
{
    protected IActionResult FhirOk(Resource resource) => new FhirContentResult(resource, StatusCodes.Status200OK);

    protected IActionResult FhirBundle(Bundle bundle) => new FhirContentResult(bundle, StatusCodes.Status200OK);

    protected IActionResult FhirNotFound(string resourceType, string id)
    {
        return new FhirContentResult(
            OperationOutcomeFactory.NotFound(resourceType, id),
            StatusCodes.Status404NotFound);
    }

    protected IActionResult FhirOperationOutcome(OperationOutcome outcome, int statusCode) => new FhirContentResult(outcome, statusCode);
}
