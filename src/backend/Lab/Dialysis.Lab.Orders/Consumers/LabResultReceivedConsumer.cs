using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Orders.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.Lab.Orders.Consumers;

/// <summary>
/// Closes the lab loop: when SmartConnect maps an inbound result back to a placed order and emits a
/// <see cref="LabResultReceivedIntegrationEvent"/>, this consumer finds the order by its placer
/// order number and records the returned observations (<see cref="LabOrder.RecordResults"/>),
/// transitioning the order to <c>Resulted</c>. An event whose placer order number we don't own is
/// ignored (another context, or a duplicate after the order was purged) — the loop is idempotent
/// because <see cref="LabOrder.RecordResults"/> replaces the result set on each delivery.
/// </summary>
public sealed class LabResultReceivedConsumer : IConsumer<LabResultReceivedIntegrationEvent>
{
    private readonly ILabOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LabResultReceivedConsumer> _logger;

    /// <summary>Creates the consumer with the order repository, unit of work, and logger.</summary>
    public LabResultReceivedConsumer(
        ILabOrderRepository orders,
        IUnitOfWork unitOfWork,
        ILogger<LabResultReceivedConsumer> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        var order = await _orders
            .FindByPlacerOrderNumberAsync(message.PlacerOrderNumber, context.CancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogInformation(
                "Lab result for unknown placer order {PlacerOrderNumber}; ignoring.",
                message.PlacerOrderNumber);
            return;
        }

        var results = message.Observations
            .Select(o => new LabResultItem(o.LoincCode, o.Display, o.Value, o.Unit, o.ReferenceRange, o.Interpretation))
            .ToList();

        order.RecordResults(results, message.FillerOrderNumber, message.ResultedAtUtc);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Recorded {Count} observation(s) on lab order {PlacerOrderNumber}.",
            results.Count,
            message.PlacerOrderNumber);
    }
}
