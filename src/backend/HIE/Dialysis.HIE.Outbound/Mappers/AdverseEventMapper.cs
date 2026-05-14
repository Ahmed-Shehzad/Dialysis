using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class AdverseEventMapper : IFhirResourceMapper<IntradialyticAdverseEventIntegrationEvent, AdverseEvent>
{
    public AdverseEvent Map(IntradialyticAdverseEventIntegrationEvent e) => new()
    {
        Id = $"{e.SessionId}-{e.EventKindCode}",
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Actuality = AdverseEvent.AdverseEventActuality.Actual,
        Event = new CodeableConcept(CodeSystems.SnomedCt, e.EventKindCode),
        Date = new FhirDateTime(e.ObservedAtUtc).ToString(),
        Severity = new CodeableConcept(null, e.Severity),
        SuspectEntity = [new AdverseEvent.SuspectEntityComponent
        {
            Instance = new ResourceReference($"Procedure/{e.SessionId}"),
        }],
    };
}
