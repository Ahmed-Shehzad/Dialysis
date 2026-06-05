using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class PatientRegisteredConsumer : IConsumer<PatientRegisteredIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly PatientMapper _mapper;
    public PatientRegisteredConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientRegisteredIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<PatientRegisteredIntegrationEvent, Patient>)_mapper,
            ConsentScopes.Demographics,
            cancellationToken: context.CancellationToken);
}

public sealed class PatientDemographicsUpdatedConsumer : IConsumer<PatientDemographicsUpdatedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly PatientMapper _mapper;
    public PatientDemographicsUpdatedConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientDemographicsUpdatedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<PatientDemographicsUpdatedIntegrationEvent, Patient>)_mapper,
            ConsentScopes.Demographics,
            cancellationToken: context.CancellationToken);
}

public sealed class PatientsMergedConsumer : IConsumer<PatientsMergedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly PatientMapper _mapper;
    public PatientsMergedConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientsMergedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.SurvivingPatientId,
            (IFhirResourceMapper<PatientsMergedIntegrationEvent, Patient>)_mapper,
            ConsentScopes.Demographics,
            cancellationToken: context.CancellationToken);
}
