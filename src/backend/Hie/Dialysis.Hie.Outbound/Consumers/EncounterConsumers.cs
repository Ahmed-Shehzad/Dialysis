using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Hie.Core.Abstraction.Consent;
using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Outbound.Dispatch;
using Dialysis.Hie.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.Outbound.Consumers;

public sealed class EncounterOpenedConsumer(OutboundQueueWriter writer, EncounterMapper mapper)
    : IConsumer<EncounterOpenedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<EncounterOpenedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirMapper<EncounterOpenedIntegrationEvent, Encounter>)mapper,
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
            (IFhirMapper<EncounterClosedIntegrationEvent, Encounter>)mapper,
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
            (IFhirMapper<ClinicalNoteSignedIntegrationEvent, DocumentReference>)mapper,
            ConsentScopes.ClinicalNotes,
            context.CancellationToken);
}
