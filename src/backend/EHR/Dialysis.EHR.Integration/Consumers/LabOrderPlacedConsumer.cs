using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Consumers;

public sealed class LabOrderPlacedConsumer(
    ILabTransmissionRepository transmissions,
    ILabGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IConsumer<LabOrderPlacedIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<LabOrderPlacedIntegrationEvent> context)
    {
        var message = context.Message;
        var id = Guid.CreateVersion7();
        var payloadDigest = $"{message.LabOrderId:N}|{string.Join(',', message.LoincPanelCodes)}";
        var transmission = LabTransmission.Queue(
            id,
            message.LabOrderId,
            message.LabFacilityCode,
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
}
