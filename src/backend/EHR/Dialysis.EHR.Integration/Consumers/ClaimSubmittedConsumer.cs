using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Consumers;

public sealed class ClaimSubmittedConsumer(
    IInsurerTransmissionRepository transmissions,
    IInsurerGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IConsumer<ClaimSubmittedIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<ClaimSubmittedIntegrationEvent> context)
    {
        var message = context.Message;
        var id = Guid.CreateVersion7();
        var payloadDigest = $"{message.ClaimId:N}|{message.BilledTotal}|{message.CurrencyCode}|{message.PayerCode}";
        var transmission = InsurerTransmission.Queue(
            id,
            message.ClaimId,
            message.PayerCode,
            message.ClaimFormatCode,
            message.ExternalControlNumber,
            payloadDigest);

        try
        {
            await gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, timeProvider.GetUtcNow().UtcDateTime);
        }

        transmissions.Add(transmission);
        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
