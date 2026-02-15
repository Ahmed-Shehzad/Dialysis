using Dialysis.Contracts.Events;
using Dialysis.Prediction.Services;
using Intercessor.Abstractions;
using Transponder.Abstractions;
using Verifier.Abstractions;
using Verifier.Exceptions;

namespace Dialysis.Prediction.Handlers;

public sealed class ObservationCreatedHandler : INotificationHandler<ObservationCreated>
{
    private readonly IRiskScorer _riskScorer;
    private readonly IVitalHistoryCache _vitalCache;
    private readonly IValidator<ObservationCreated> _validator;
    private readonly IPublishEndpoint _publishEndpoint;

    public ObservationCreatedHandler(
        IRiskScorer riskScorer,
        IVitalHistoryCache vitalCache,
        IValidator<ObservationCreated> validator,
        IPublishEndpoint publishEndpoint)
    {
        _riskScorer = riskScorer;
        _vitalCache = vitalCache;
        _validator = validator;
        _publishEndpoint = publishEndpoint;
    }

    public async Task HandleAsync(ObservationCreated notification, CancellationToken cancellationToken = default)
    {
        var result = await _validator.ValidateAsync(notification, cancellationToken);
        if (result.Errors.Count > 0)
            throw new ValidationException(result.Errors);

        var value = double.TryParse(notification.Value, out var v) ? v : 0;
        var vital = new VitalSnapshot(notification.Code, value, notification.Effective);
        _vitalCache.Append(notification.PatientId, vital);

        var recentVitals = _vitalCache.GetRecent(notification.PatientId, 20);
        var riskScore = _riskScorer.CalculateRisk(notification.PatientId, recentVitals);

        if (riskScore >= 0.6)
        {
            await _publishEndpoint.PublishAsync(new HypotensionRiskRaised(
                notification.CorrelationId,
                notification.TenantId,
                notification.PatientId,
                notification.EncounterId,
                riskScore,
                DateTimeOffset.UtcNow
            ), cancellationToken);
        }
    }
}
