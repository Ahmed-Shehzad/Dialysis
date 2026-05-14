using Dialysis.EHR.Contracts.Integration;
using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.Outbound.Mappers;

public sealed class ClinicalNoteMapper : IFhirMapper<ClinicalNoteSignedIntegrationEvent, DocumentReference>
{
    public DocumentReference Map(ClinicalNoteSignedIntegrationEvent e) => new()
    {
        Id = e.NoteId.ToString(),
        Status = DocumentReferenceStatus.Current,
        Type = new CodeableConcept(CodeSystems.Loinc, CodeSystems.LoincClinicalNoteTypeCode, "Subsequent evaluation note"),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Date = new DateTimeOffset(DateTime.SpecifyKind(e.SignedAtUtc, DateTimeKind.Utc)),
        Author = [new ResourceReference($"Practitioner/{e.SignedByProviderId}")],
        Context = new DocumentReference.ContextComponent
        {
            Encounter = [new ResourceReference($"Encounter/{e.EncounterId}")],
        },
    };
}
