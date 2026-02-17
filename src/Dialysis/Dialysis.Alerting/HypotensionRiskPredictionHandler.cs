using Dialysis.Contracts.Events;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alerting;

/// <summary>
/// Evaluates hypotension risk from ObservationCreated. Publishes HypotensionRiskRaised when BP is low.
/// </summary>
public sealed class HypotensionRiskPredictionHandler : INotificationHandler<ObservationCreated>
{
    private readonly IPublisher _publisher;
    private readonly ILogger<HypotensionRiskPredictionHandler> _logger;

    private const decimal HypotensionSystolicThreshold = 90m;

    public HypotensionRiskPredictionHandler(IPublisher publisher, ILogger<HypotensionRiskPredictionHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task HandleAsync(ObservationCreated notification, CancellationToken cancellationToken = default)
    {
        if (!IsBloodPressureObservation(notification))
            return;

        var value = notification.NumericValue;
        if (!value.HasValue)
            return;

        if (value.Value >= HypotensionSystolicThreshold)
            return;

        var reason = $"Systolic BP {value} mmHg below threshold ({HypotensionSystolicThreshold})";
        _logger.LogWarning(
            "Hypotension risk: PatientId={PatientId}, ObservationId={ObservationId}, Value={Value}",
            notification.PatientId.Value,
            notification.ObservationId.Value,
            value);

        var evt = new HypotensionRiskRaised(
            notification.ObservationId,
            notification.PatientId,
            notification.TenantId,
            reason,
            value,
            null);

        await _publisher.PublishAsync(evt, cancellationToken);
    }

    private static bool IsBloodPressureObservation(ObservationCreated n) =>
        n.LoincCode.Value == LoincCode.BloodPressure.Value;
}
