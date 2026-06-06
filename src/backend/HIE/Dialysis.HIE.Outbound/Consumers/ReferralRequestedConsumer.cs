using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Outbound.CareSummary;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Outbound.Consumers;

/// <summary>
/// Transfer-of-care trigger: when a clinician refers a patient out, assemble a CCD and push it to
/// the receiving organisation over Directed Exchange. Reuses the <see cref="CareSummaryAssembler"/>
/// (consent- and purpose-gated) with the referral's explicit destination partner.
/// </summary>
public sealed class ReferralRequestedConsumer : IConsumer<ReferralRequestedIntegrationEvent>
{
    private readonly CareSummaryAssembler _assembler;
    public ReferralRequestedConsumer(CareSummaryAssembler assembler) => _assembler = assembler;

    public Task HandleAsync(ConsumeContext<ReferralRequestedIntegrationEvent> context) =>
        _assembler.AssembleAndEnqueueAsync(
            context.Message.PatientId,
            purposeOfUse: TefcaPermittedPurposes.Treatment,
            destinationPartnerId: context.Message.DestinationPartnerId,
            cancellationToken: context.CancellationToken);
}
