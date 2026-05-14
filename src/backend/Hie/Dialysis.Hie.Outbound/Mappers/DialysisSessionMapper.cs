using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Core.Coding;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.Outbound.Mappers;

public sealed class DialysisSessionMapper :
    IFhirMapper<DialysisSessionStartedIntegrationEvent, Procedure>,
    IFhirMapper<DialysisSessionCompletedIntegrationEvent, Procedure>,
    IFhirMapper<DialysisSessionAbortedIntegrationEvent, Procedure>
{
    public Procedure Map(DialysisSessionStartedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.InProgress,
        Code = HaemodialysisConcept(),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { StartElement = new FhirDateTime(e.StartedAtUtc) },
        UsedCode = [new CodeableConcept(null, e.DialyzerModel)],
    };

    public Procedure Map(DialysisSessionCompletedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.Completed,
        Code = HaemodialysisConcept(),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { EndElement = new FhirDateTime(e.CompletedAtUtc) },
        Outcome = new CodeableConcept(CodeSystems.SnomedCt, "385669000", "Successful"),
        Note = [new Annotation { Text = new Markdown($"Achieved UF {e.AchievedUfVolumeLiters} L over {e.ActualDurationMinutes} min") }],
    };

    public Procedure Map(DialysisSessionAbortedIntegrationEvent e) => new()
    {
        Id = e.SessionId.ToString(),
        Status = EventStatus.Stopped,
        Code = HaemodialysisConcept(),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Performed = new Period { EndElement = new FhirDateTime(e.AbortedAtUtc) },
        StatusReason = new CodeableConcept(null, e.ReasonCode),
    };

    private static CodeableConcept HaemodialysisConcept() =>
        new(CodeSystems.SnomedCt, CodeSystems.SnomedHaemodialysisCode, "Renal dialysis");
}
