using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Consumers;

public sealed class LabOrderPlacedConsumer : IConsumer<LabOrderPlacedIntegrationEvent>
{
    private readonly ILabTransmissionRepository _transmissions;
    private readonly ILabGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public LabOrderPlacedConsumer(ILabTransmissionRepository transmissions,
        ILabGateway gateway,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _transmissions = transmissions;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
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
            var controlNumber = await _gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(controlNumber, _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, _timeProvider.GetUtcNow().UtcDateTime);
        }

        _transmissions.Add(transmission);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
