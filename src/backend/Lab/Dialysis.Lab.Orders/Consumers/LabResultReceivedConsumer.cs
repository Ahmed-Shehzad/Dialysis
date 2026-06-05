using Dialysis.BuildingBlocks.Fhir.Terminology;
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
    private readonly IDialysisCodeValidator _codeValidator;
    private readonly ILogger<LabResultReceivedConsumer> _logger;

    /// <summary>Creates the consumer with the order repository, unit of work, code validator, and logger.</summary>
    public LabResultReceivedConsumer(
        ILabOrderRepository orders,
        IUnitOfWork unitOfWork,
        IDialysisCodeValidator codeValidator,
        ILogger<LabResultReceivedConsumer> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _codeValidator = codeValidator;
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

        var results = new List<LabResultItem>(message.Observations.Count);
        foreach (var o in message.Observations)
        {
            var (code, display) = await NormalizeCodeAsync(
                message.PlacerOrderNumber, o.LoincCode, o.Display, context.CancellationToken).ConfigureAwait(false);
            results.Add(new LabResultItem(code, display, o.Value, o.Unit, o.ReferenceRange, o.Interpretation));
        }

        order.RecordResults(results, message.FillerOrderNumber, message.ResultedAtUtc);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Recorded {Count} observation(s) on lab order {PlacerOrderNumber}.",
            results.Count,
            message.PlacerOrderNumber);
    }

    /// <summary>
    /// Coding governance at the result boundary: validate the observation code against the governed
    /// dialysis lab panel (LOINC). A code that isn't in the panel is treated as a possible local-lab
    /// mnemonic and run through the local→LOINC concept map; a successful translate normalises it to the
    /// LOINC equivalent. A code that is neither governed nor translatable is recorded as-is but logged as
    /// non-conformant so the gap is visible — clinical data is never dropped.
    /// </summary>
    private async Task<(string Code, string Display)> NormalizeCodeAsync(
        string placerOrderNumber, string code, string display, CancellationToken cancellationToken)
    {
        var validation = await _codeValidator
            .ValidateAsync(DialysisTerminologyCatalog.DialysisLabPanelValueSet, code, DialysisTerminologyCatalog.LoincSystem, cancellationToken)
            .ConfigureAwait(false);
        if (validation.IsValid)
        {
            return (code, display);
        }

        var translation = await _codeValidator
            .TranslateAsync(DialysisTerminologyCatalog.LocalLabToLoincConceptMap, DialysisTerminologyCatalog.LocalLabSystem, code, cancellationToken)
            .ConfigureAwait(false);
        if (translation is not null)
        {
            _logger.LogInformation(
                "Normalized local lab code {LocalCode} to LOINC {LoincCode} on order {PlacerOrderNumber}.",
                code, translation.TargetCode, placerOrderNumber);
            return (translation.TargetCode, translation.TargetDisplay ?? display);
        }

        _logger.LogWarning(
            "Lab result code {Code} on order {PlacerOrderNumber} is not in the governed dialysis lab panel and has no local→LOINC mapping; recording as-is.",
            code, placerOrderNumber);
        return (code, display);
    }
}
