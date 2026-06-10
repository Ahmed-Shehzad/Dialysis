using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

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
    public Task HandleAsync(ConsumeContext<EncounterOpenedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            (IFhirResourceMapper<EncounterOpenedIntegrationEvent, Encounter>)_mapper,
            ConsentScopes.Encounters,
            cancellationToken: context.CancellationToken);
}

public sealed class EncounterClosedConsumer : IConsumer<EncounterClosedIntegrationEvent>
{
    private readonly OutboundQueueWriter _writer;
    private readonly EncounterMapper _mapper;
    private readonly CareSummaryAssembler _careSummary;
    private readonly IPartnerRouter _partnerRouter;
    private readonly OutboundOptions _options;
    public EncounterClosedConsumer(
        OutboundQueueWriter writer,
        EncounterMapper mapper,
        CareSummaryAssembler careSummary,
        IPartnerRouter partnerRouter,
        IOptions<OutboundOptions> options)
    {
        _writer = writer;
        _mapper = mapper;
        _careSummary = careSummary;
        _partnerRouter = partnerRouter;
        _options = options.Value;
    }

    public async Task HandleAsync(ConsumeContext<EncounterClosedIntegrationEvent> context)
    {
        var message = context.Message;
        await _writer.EnqueueAsync(
            message,
            message.PatientId,
            (IFhirResourceMapper<EncounterClosedIntegrationEvent, Encounter>)_mapper,
            ConsentScopes.Encounters,
            cancellationToken: context.CancellationToken).ConfigureAwait(false);

        // Transition-of-care: on discharge, optionally push a CCD care summary to the patient's
        // care-network partner(s) — sharing the discharge picture without a manual referral.
        if (!_options.AutoDischargeSummary)
            return;
        foreach (var partnerId in _partnerRouter.ResolvePartners(message.PatientId, ConsentScopes.ClinicalNotes))
        {
            await _careSummary
                .AssembleAndEnqueueAsync(message.PatientId, destinationPartnerId: partnerId, cancellationToken: context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
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
    public Task HandleAsync(ConsumeContext<ClinicalNoteSignedIntegrationEvent> context) =>
        _writer.EnqueueAsync(
            context.Message,
            context.Message.PatientId,
            _mapper,
            ConsentScopes.ClinicalNotes,
            cancellationToken: context.CancellationToken);
}
