using global::Hl7.Fhir.Model;

using Intercessor.Abstractions;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Gateway.Features.Fhir;

public sealed class GetFhirMetadataQueryHandler : IQueryHandler<GetFhirMetadataQuery, GetFhirMetadataResult>
{
    public Task<GetFhirMetadataResult> HandleAsync(GetFhirMetadataQuery request, CancellationToken cancellationToken = default)
    {
        var baseUrl = request.BaseUrl.TrimEnd('/');
        var cs = BuildCapabilityStatement(baseUrl);
        var json = FhirMappers.ToFhirJson(cs);
        return Task.FromResult(new GetFhirMetadataResult(json, "application/fhir+json"));
    }

    private static CapabilityStatement BuildCapabilityStatement(string baseUrl)
    {
        return new CapabilityStatement
        {
            Id = "dialysis-pdms",
            Url = $"{baseUrl}/metadata",
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
                    Documentation = "Dialysis PDMS FHIR R4 REST API. Supports Patient, Observation, Procedure (dialysis sessions).",
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
                        },
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Procedure",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Procedure",
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
                                    Documentation = "Dialysis sessions by patient"
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
