using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class EncounterOpenedConsumer(OutboundQueueWriter writer, EncounterMapper mapper)
    : IConsumer<EncounterOpenedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<EncounterOpenedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<EncounterOpenedIntegrationEvent, Encounter>)mapper,
            ConsentScopes.Encounters,
            context.CancellationToken);
}

public sealed class EncounterClosedConsumer(OutboundQueueWriter writer, EncounterMapper mapper)
    : IConsumer<EncounterClosedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<EncounterClosedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<EncounterClosedIntegrationEvent, Encounter>)mapper,
            ConsentScopes.Encounters,
            context.CancellationToken);
}

public sealed class ClinicalNoteSignedConsumer(OutboundQueueWriter writer, ClinicalNoteMapper mapper)
    : IConsumer<ClinicalNoteSignedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<ClinicalNoteSignedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<ClinicalNoteSignedIntegrationEvent, DocumentReference>)mapper,
            ConsentScopes.ClinicalNotes,
            context.CancellationToken);
}
