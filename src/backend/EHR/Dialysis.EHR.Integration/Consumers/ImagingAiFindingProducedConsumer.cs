using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Attaches an advisory AI imaging finding to its order (matched by accession), pending clinician
/// sign-off. No-op when the accession is unknown, or when the order has already been reviewed
/// (RecordAiFinding preserves the human decision) — so per-instance producer events are idempotent.
/// </summary>
public sealed class ImagingAiFindingProducedConsumer : IConsumer<ImagingAiFindingProducedIntegrationEvent>
{
    private readonly IImagingOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImagingAiFindingProducedConsumer> _logger;

    public ImagingAiFindingProducedConsumer(
        IImagingOrderRepository orders,
        IUnitOfWork unitOfWork,
        ILogger<ImagingAiFindingProducedConsumer> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeContext<ImagingAiFindingProducedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        var order = await _orders
            .GetByAccessionNumberAsync(message.AccessionNumber, context.CancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogInformation(
                "AI imaging finding for unknown accession {AccessionNumber}; ignoring.",
                message.AccessionNumber);
            return;
        }

        var recorded = order.RecordAiFinding(
            message.ModelId,
            message.FindingCode,
            message.FindingSystem,
            message.FindingDisplay,
            message.Confidence,
            message.Interpretation,
            message.Summary);

        if (!recorded)
        {
            return; // already reviewed — preserve the clinician's decision
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Attached AI finding {Code} ({ModelId}) to imaging order {AccessionNumber}, pending review.",
            message.FindingCode, message.ModelId, message.AccessionNumber);
    }
}
