using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class DialysisSessionMapper :
    IFhirResourceMapper<DialysisSessionStartedIntegrationEvent, Procedure>,
    IFhirResourceMapper<DialysisSessionCompletedIntegrationEvent, Procedure>,
    IFhirResourceMapper<DialysisSessionAbortedIntegrationEvent, Procedure>
{
    private readonly IConceptCatalog _concepts;
    public DialysisSessionMapper(IConceptCatalog concepts) => _concepts = concepts;
    public Procedure Map(DialysisSessionStartedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.InProgress,
        Code = _concepts.Get(ClinicalConcepts.RenalDialysis),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { StartElement = new FhirDateTime(e.StartedAtUtc) },
        UsedCode = [new CodeableConcept { Text = e.DialyzerModel }],
    };

    public Procedure Map(DialysisSessionCompletedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.Completed,
        Code = _concepts.Get(ClinicalConcepts.RenalDialysis),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { EndElement = new FhirDateTime(e.CompletedAtUtc) },
        Outcome = _concepts.Get(ClinicalConcepts.SuccessfulOutcome),
        Note = [new Annotation { Text = new Markdown($"Achieved UF {e.AchievedUfVolumeLiters} L over {e.ActualDurationMinutes} min") }],
    };

    public Procedure Map(DialysisSessionAbortedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.Stopped,
        Code = _concepts.Get(ClinicalConcepts.RenalDialysis),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { EndElement = new FhirDateTime(e.AbortedAtUtc) },
        StatusReason = new CodeableConcept { Text = e.ReasonCode },
    };
}
