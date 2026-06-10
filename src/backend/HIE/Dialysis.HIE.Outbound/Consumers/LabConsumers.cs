using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.PublicHealth;
using Task = System.Threading.Tasks.Task;

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
    public Task HandleAsync(ConsumeContext<LabOrderPlacedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            _mapper,
            ConsentScopes.Labs,
            cancellationToken: context.CancellationToken);
}

public sealed class LabResultReceivedConsumer : IConsumer<LabResultReceivedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly LabResultMapper _mapper;
    private readonly PublicHealthReporter _publicHealth;
    public LabResultReceivedConsumer(OutboundQueueWriter writer, LabResultMapper mapper, PublicHealthReporter publicHealth)
    {
        _writer = writer;
        _mapper = mapper;
        _publicHealth = publicHealth;
    }
    public async Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context)
    {
        var message = context.Message;
        await _writer.EnqueueAsync(
            message,
            message.PatientId,
            _mapper,
            ConsentScopes.Labs,
            cancellationToken: context.CancellationToken).ConfigureAwait(false);

        // Electronic case reporting: a reportable LOINC finding is reported to the public-health
        // authority (mandated; consent-bypassed inside the reporter). No-op until configured.
        await _publicHealth
            .ReportAsync(message.PatientId, _mapper.Map(message), message.LoincCode, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
