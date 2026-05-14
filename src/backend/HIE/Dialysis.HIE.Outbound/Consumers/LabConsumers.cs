using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class LabOrderPlacedConsumer(OutboundQueueWriter writer, LabOrderMapper mapper)
    : IConsumer<LabOrderPlacedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<LabOrderPlacedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<LabOrderPlacedIntegrationEvent, ServiceRequest>)mapper,
            ConsentScopes.Labs,
            context.CancellationToken);
}

public sealed class LabResultReceivedConsumer(OutboundQueueWriter writer, LabResultMapper mapper)
    : IConsumer<LabResultReceivedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<LabResultReceivedIntegrationEvent, Observation>)mapper,
            ConsentScopes.Labs,
            context.CancellationToken);
}
