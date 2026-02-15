using Dialysis.ApiClients;
using Dialysis.HisIntegration.Features.AdtSync;
using Hl7.Fhir.Model;

namespace Dialysis.HisIntegration.Services;

public interface IProvenanceRecorder
{
    System.Threading.Tasks.Task RecordAdtProvenanceAsync(string targetResourceId, string targetResourceType, string activity, AdtParsedData source, CancellationToken cancellationToken = default);
}

public sealed class ProvenanceRecorder : IProvenanceRecorder
{
    private readonly IFhirApiFactory _fhirApiFactory;
    private readonly ITenantFhirResolver _resolver;

    public ProvenanceRecorder(IFhirApiFactory fhirApiFactory, ITenantFhirResolver resolver)
    {
        _fhirApiFactory = fhirApiFactory;
        _resolver = resolver;
    }

    public async System.Threading.Tasks.Task RecordAdtProvenanceAsync(string targetResourceId, string targetResourceType, string activity, AdtParsedData source, CancellationToken cancellationToken = default)
    {
        var baseUrl = _resolver.GetBaseUrl(null);
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(targetResourceId))
            return;

        var provenance = new Provenance
        {
            Recorded = DateTimeOffset.UtcNow,
            Target = [new ResourceReference($"{targetResourceType}/{targetResourceId}")],
            Activity = new CodeableConcept
            {
                Coding = [new Coding("http://hl7.org/fhir/CodeSystem/v3-DataOperation", "CREATE", "create")]
            },
            Agent =
            [
                new Provenance.AgentComponent
                {
                    Type = new CodeableConcept { Coding = [new Coding("http://terminology.hl7.org/CodeSystem/provenance-participant-type", "assembler", "Assembler")] },
                    Who = new ResourceReference($"urn:oid:2.16.840.1.113883.4.1&{source.Mrn ?? "unknown"}")
                }
            ],
            Entity =
            [
                new Provenance.EntityComponent
                {
                    Role = Provenance.ProvenanceEntityRole.Source,
                    What = new ResourceReference($"urn:adt:message:{source.MessageType}:{source.Mrn ?? "unknown"}")
                }
            ],
            Meta = new Meta { Profile = ["http://hl7.org/fhir/StructureDefinition/Provenance"] }
        };

        var api = _fhirApiFactory.ForBaseUrl(baseUrl);
        var response = await api.CreateProvenance(provenance, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
