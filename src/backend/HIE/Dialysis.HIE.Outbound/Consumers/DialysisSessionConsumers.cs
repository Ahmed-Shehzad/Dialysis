using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class DialysisSessionStartedConsumer : IConsumer<DialysisSessionStartedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly DialysisSessionMapper _mapper;
    public DialysisSessionStartedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionStartedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionStartedIntegrationEvent, Procedure>)_mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class DialysisSessionCompletedConsumer : IConsumer<DialysisSessionCompletedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly DialysisSessionMapper _mapper;
    public DialysisSessionCompletedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionCompletedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionCompletedIntegrationEvent, Procedure>)_mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class DialysisSessionAbortedConsumer : IConsumer<DialysisSessionAbortedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly DialysisSessionMapper _mapper;
    public DialysisSessionAbortedConsumer(OutboundQueueWriter writer, DialysisSessionMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<DialysisSessionAbortedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<DialysisSessionAbortedIntegrationEvent, Procedure>)_mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}

public sealed class IntradialyticAdverseEventConsumer : IConsumer<IntradialyticAdverseEventIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly AdverseEventMapper _mapper;
    public IntradialyticAdverseEventConsumer(OutboundQueueWriter writer, AdverseEventMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<IntradialyticAdverseEventIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<IntradialyticAdverseEventIntegrationEvent, AdverseEvent>)_mapper,
            ConsentScopes.DialysisSessions,
            context.CancellationToken);
}
