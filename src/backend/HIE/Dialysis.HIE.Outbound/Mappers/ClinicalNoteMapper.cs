using Dialysis.EHR.Contracts.Integration;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class ClinicalNoteMapper(IConceptCatalog concepts) : IFhirResourceMapper<ClinicalNoteSignedIntegrationEvent, DocumentReference>
{
    public DocumentReference Map(ClinicalNoteSignedIntegrationEvent e) => new()
    {
        Id = e.NoteId.ToString(),
        Status = DocumentReferenceStatus.Current,
        Type = concepts.Get(ClinicalConcepts.SubsequentEvaluationNote),
        Subject = new ResourceReference($"Patient/{e.PatientId}"),
        Date = new DateTimeOffset(DateTime.SpecifyKind(e.SignedAtUtc, DateTimeKind.Utc)),
        Author = [new ResourceReference($"Practitioner/{e.SignedByProviderId}")],
        Context = new DocumentReference.ContextComponent
        {
            Encounter = [new ResourceReference($"Encounter/{e.EncounterId}")],
        },
    };
}
