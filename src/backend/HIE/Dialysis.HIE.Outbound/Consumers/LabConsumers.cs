using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class LabOrderPlacedConsumer : IConsumer<LabOrderPlacedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly LabOrderMapper _mapper;
    public LabOrderPlacedConsumer(OutboundQueueWriter writer, LabOrderMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<LabOrderPlacedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<LabOrderPlacedIntegrationEvent, ServiceRequest>)_mapper,
            ConsentScopes.Labs,
            context.CancellationToken);
}

public sealed class LabResultReceivedConsumer : IConsumer<LabResultReceivedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly LabResultMapper _mapper;
    public LabResultReceivedConsumer(OutboundQueueWriter writer, LabResultMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<LabResultReceivedIntegrationEvent, Observation>)_mapper,
            ConsentScopes.Labs,
            context.CancellationToken);
}
