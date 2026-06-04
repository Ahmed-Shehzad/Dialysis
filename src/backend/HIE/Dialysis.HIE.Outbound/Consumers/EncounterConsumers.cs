using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class EncounterOpenedConsumer : IConsumer<EncounterOpenedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly EncounterMapper _mapper;
    public EncounterOpenedConsumer(OutboundQueueWriter writer, EncounterMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<EncounterOpenedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<EncounterOpenedIntegrationEvent, Encounter>)_mapper,
            ConsentScopes.Encounters,
            context.CancellationToken);
}

public sealed class EncounterClosedConsumer : IConsumer<EncounterClosedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly EncounterMapper _mapper;
    public EncounterClosedConsumer(OutboundQueueWriter writer, EncounterMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<EncounterClosedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<EncounterClosedIntegrationEvent, Encounter>)_mapper,
            ConsentScopes.Encounters,
            context.CancellationToken);
}

public sealed class ClinicalNoteSignedConsumer : IConsumer<ClinicalNoteSignedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly ClinicalNoteMapper _mapper;
    public ClinicalNoteSignedConsumer(OutboundQueueWriter writer, ClinicalNoteMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<ClinicalNoteSignedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<ClinicalNoteSignedIntegrationEvent, DocumentReference>)_mapper,
            ConsentScopes.ClinicalNotes,
            context.CancellationToken);
}
