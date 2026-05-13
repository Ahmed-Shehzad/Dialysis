using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Reacts to <see cref="PrescriptionOrderedIntegrationEvent"/> by queueing an outbound NCPDP SCRIPT
/// transmission to the pharmacy and invoking the wire gateway.
/// </summary>
public sealed class PrescriptionOrderedConsumer(
    IPharmacyTransmissionRepository transmissions,
    IPharmacyGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IConsumer<PrescriptionOrderedIntegrationEvent>
{
    public async Task Handle(ConsumeContext<PrescriptionOrderedIntegrationEvent> context)
    {
        var message = context.Message;
        var id = Guid.CreateVersion7();
        var payloadDigest = ComputeDigest(message);
        var transmission = PharmacyTransmission.Queue(
            id,
            message.PrescriptionId,
            message.PharmacyNcpdpId,
            message.TransmissionFormat,
            payloadDigest);

        try
        {
            var controlNumber = await gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(controlNumber, timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, timeProvider.GetUtcNow().UtcDateTime);
        }

        transmissions.Add(transmission);
        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private static string ComputeDigest(PrescriptionOrderedIntegrationEvent message) =>
        $"{message.PrescriptionId:N}|{message.MedicationRxnormCode}|{message.DoseText}|{message.FrequencyText}|{message.QuantityDispensed}";
}
