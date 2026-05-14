using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Consumers;

public sealed class PatientRegisteredConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    : IConsumer<PatientRegisteredIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientRegisteredIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<PatientRegisteredIntegrationEvent, Patient>)mapper,
            ConsentScopes.Demographics,
            context.CancellationToken);
}

public sealed class PatientDemographicsUpdatedConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    : IConsumer<PatientDemographicsUpdatedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientDemographicsUpdatedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<PatientDemographicsUpdatedIntegrationEvent, Patient>)mapper,
            ConsentScopes.Demographics,
            context.CancellationToken);
}

public sealed class PatientsMergedConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    : IConsumer<PatientsMergedIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientsMergedIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.SurvivingPatientId,
            (IFhirResourceMapper<PatientsMergedIntegrationEvent, Patient>)mapper,
            ConsentScopes.Demographics,
            context.CancellationToken);
}
