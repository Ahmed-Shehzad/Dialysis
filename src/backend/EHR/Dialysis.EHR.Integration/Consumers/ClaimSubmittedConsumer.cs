using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Consumers;

public sealed class ClaimSubmittedConsumer : IConsumer<ClaimSubmittedIntegrationEvent>
{
    private readonly IInsurerTransmissionRepository _transmissions;
    private readonly IInsurerGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public ClaimSubmittedConsumer(IInsurerTransmissionRepository transmissions,
        IInsurerGateway gateway,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _transmissions = transmissions;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
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
            await _gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, _timeProvider.GetUtcNow().UtcDateTime);
        }

        _transmissions.Add(transmission);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
