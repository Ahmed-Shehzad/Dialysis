using Asp.Versioning;

using Dialysis.Gateway.Features.Fhir;

using global::Hl7.Fhir.Model;

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
    /// <summary>
    /// CapabilityStatement describing this server's FHIR R4 support. EHR clients use this for discovery.
    /// </summary>
    [HttpGet("metadata")]
    [Produces("application/fhir+json", "application/json")]
    public IActionResult Metadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/fhir/r4/";
        var cs = BuildCapabilityStatement(baseUrl);
        var json = FhirMappers.ToFhirJson(cs);
        return Content(json, "application/fhir+json");
    }

    private static CapabilityStatement BuildCapabilityStatement(string baseUrl)
    {
        return new CapabilityStatement
        {
            Id = "dialysis-pdms",
            Url = $"{baseUrl.TrimEnd('/')}/metadata",
            Version = "1.0",
            Name = "Dialysis PDMS FHIR Server",
            Status = PublicationStatus.Active,
            Date = "2025-02-17",
            Kind = CapabilityStatementKind.Capability,
            Software = new CapabilityStatement.SoftwareComponent
            {
                Name = "Dialysis PDMS",
                Version = "1.0"
            },
            FhirVersion = FHIRVersion.N4_0_1,
            Format = ["json", "application/fhir+json"],
            Rest =
            [
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Documentation = "Dialysis PDMS FHIR R4 REST API. Supports Patient and Observation.",
                    Resource =
                    [
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Patient",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                            Interaction =
                            [
                                new CapabilityStatement.ResourceInteractionComponent
                                    { Code = CapabilityStatement.TypeRestfulInteraction.Read },
                                new CapabilityStatement.ResourceInteractionComponent
                                    { Code = CapabilityStatement.TypeRestfulInteraction.Create }
                            ]
                        },
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Observation",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Observation",
                            Interaction =
                            [
                                new CapabilityStatement.ResourceInteractionComponent
                                    { Code = CapabilityStatement.TypeRestfulInteraction.Read },
                                new CapabilityStatement.ResourceInteractionComponent
                                    { Code = CapabilityStatement.TypeRestfulInteraction.SearchType }
                            ],
                            SearchParam =
                            [
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "patient",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by patient reference"
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
