using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class DialysisSessionStartedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    : IConsumer<DialysisSessionStartedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionStartedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionStartedIntegrationEvent, Procedure>)mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class DialysisSessionCompletedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    : IConsumer<DialysisSessionCompletedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionCompletedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionCompletedIntegrationEvent, Procedure>)mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class DialysisSessionAbortedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    : IConsumer<DialysisSessionAbortedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionAbortedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionAbortedIntegrationEvent, Procedure>)mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class IntradialyticAdverseEventConsumer(OutboundQueueWriter writer, AdverseEventMapper mapper)
    : IConsumer<IntradialyticAdverseEventIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<IntradialyticAdverseEventIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<IntradialyticAdverseEventIntegrationEvent, AdverseEvent>)mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}
