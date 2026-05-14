using Dialysis.EHR.Contracts.Integration;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class EncounterMapper :
    IFhirResourceMapper<EncounterOpenedIntegrationEvent, Encounter>,
    IFhirResourceMapper<EncounterClosedIntegrationEvent, Encounter>
{
    public Encounter Map(EncounterOpenedIntegrationEvent e) => new()
    {
        Id = e.EncounterId.ToString(),
        Status = Encounter.EncounterStatus.InProgress,
        Class = new Coding(CodeSystems.SnomedCt, e.EncounterClassCode),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Period = new Period { StartElement = new FhirDateTime(e.StartedAtUtc) },
        Participant =
        [
            new Encounter.ParticipantComponent
            {
                Individual = new ResourceReference($"Practitioner/{e.ProviderId}"),
            },
        ],
    };

    public Encounter Map(EncounterClosedIntegrationEvent e)
    {
        var encounter = new Encounter
        {
            Id = e.EncounterId.ToString(),
            Status = Encounter.EncounterStatus.Finished,
            Subject = new ResourceReference($"Patient/{e.PatientId}"),
            Period = new Period { EndElement = new FhirDateTime(e.ClosedAtUtc) },
            Participant =
            [
                new Encounter.ParticipantComponent
                {
                    Individual = new ResourceReference($"Practitioner/{e.ProviderId}"),
                },
            ],
        };
        foreach (var icd in e.DiagnosisIcd10Codes)
        {
            encounter.ReasonCode.Add(new CodeableConcept(CodeSystems.Icd10, icd));
        }
        return encounter;
    }
}
