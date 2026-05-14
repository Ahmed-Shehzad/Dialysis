using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Hie.Core.Abstraction.Consent;
using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Outbound.Dispatch;
using Dialysis.Hie.Outbound.Mappers;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.Outbound.Consumers;

public sealed class PatientRegisteredConsumer(OutboundQueueWriter writer, PatientMapper mapper)
    : IConsumer<PatientRegisteredIntegrationEvent>
{
    public System.Threading.Tasks.Task HandleAsync(ConsumeContext<PatientRegisteredIntegrationEvent> context) =>
        writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirMapper<PatientRegisteredIntegrationEvent, Patient>)mapper,
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
            (IFhirMapper<PatientDemographicsUpdatedIntegrationEvent, Patient>)mapper,
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
            (IFhirMapper<PatientsMergedIntegrationEvent, Patient>)mapper,
            ConsentScopes.Demographics,
            context.CancellationToken);
}
