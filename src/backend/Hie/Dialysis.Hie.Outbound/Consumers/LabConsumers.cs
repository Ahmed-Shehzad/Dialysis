using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Hie.Core.Abstraction.Consent;
using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Outbound.Dispatch;
using Dialysis.Hie.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.Outbound.Consumers;

public sealed class LabOrderPlacedConsumer(OutboundQueueWriter writer, LabOrderMapper mapper)
    : IConsumer<LabOrderPlacedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<LabOrderPlacedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirMapper<LabOrderPlacedIntegrationEvent, ServiceRequest>)mapper,
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
            (IFhirMapper<LabResultReceivedIntegrationEvent, Observation>)mapper,
            ConsentScopes.Labs,
            context.CancellationToken);
}
