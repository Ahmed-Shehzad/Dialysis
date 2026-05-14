using Dialysis.EHR.Contracts.Integration;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class LabOrderMapper : IFhirResourceMapper<LabOrderPlacedIntegrationEvent, ServiceRequest>
{
    public ServiceRequest Map(LabOrderPlacedIntegrationEvent e)
    {
        var request = new ServiceRequest
        {
            Id = e.LabOrderId.ToString(),
            Status = RequestStatus.Active,
            Intent = RequestIntent.Order,
            Subject = new ResourceReference($"Patient/{e.PatientId}"),
            Encounter = new ResourceReference($"Encounter/{e.EncounterId}"),
            Requester = new ResourceReference($"Practitioner/{e.OrderingProviderId}"),
            PerformerType = new CodeableConcept(null, e.LabFacilityCode),
        };
        var code = new CodeableConcept();
        foreach (var loinc in e.LoincPanelCodes)
        {
            code.Coding.Add(new Coding(CodeSystems.Loinc, loinc));
        }
        request.Code = code;
        return request;
    }
}
