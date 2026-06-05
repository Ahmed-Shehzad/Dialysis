using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Closes the imaging loop: when an imaging study is correlated back to its order (by accession
/// number — e.g. after SmartConnect DICOM receives the STOW'd study and emits
/// <see cref="ImagingStudyLinkedIntegrationEvent"/>), this consumer records the study instance UID
/// on the order and completes it. A study whose accession number we don't own is a logged no-op.
/// </summary>
public sealed class ImagingStudyLinkedConsumer : IConsumer<ImagingStudyLinkedIntegrationEvent>
{
    private readonly IImagingOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImagingStudyLinkedConsumer> _logger;

    public ImagingStudyLinkedConsumer(
        IImagingOrderRepository orders,
        IUnitOfWork unitOfWork,
        ILogger<ImagingStudyLinkedConsumer> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeContext<ImagingStudyLinkedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        var order = await _orders
            .GetByAccessionNumberAsync(message.AccessionNumber, context.CancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogInformation(
                "Imaging study for unknown accession {AccessionNumber}; ignoring.",
                message.AccessionNumber);
            return;
        }

        order.LinkStudy(message.StudyInstanceUid);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Linked study {StudyInstanceUid} to imaging order {AccessionNumber}.",
            message.StudyInstanceUid,
            message.AccessionNumber);
    }
}
