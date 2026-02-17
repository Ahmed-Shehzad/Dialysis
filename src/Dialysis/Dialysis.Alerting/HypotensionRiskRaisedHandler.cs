using Dialysis.Contracts.Events;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alerting;

/// <summary>
/// Creates an Alert when HypotensionRiskRaised is published.
/// </summary>
public sealed class HypotensionRiskRaisedHandler : INotificationHandler<HypotensionRiskRaised>
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<HypotensionRiskRaisedHandler> _logger;

    public HypotensionRiskRaisedHandler(IAlertRepository alertRepository, ILogger<HypotensionRiskRaisedHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task HandleAsync(HypotensionRiskRaised notification, CancellationToken cancellationToken = default)
    {
        var alert = Alert.Create(
            notification.TenantId,
            notification.PatientId,
            notification.Reason,
            notification.ObservationId,
            "Warning");

        await _alertRepository.AddAsync(alert, cancellationToken);

        _logger.LogInformation(
            "Alert created: AlertId={AlertId}, PatientId={PatientId}, Reason={Reason}",
            alert.Id,
            notification.PatientId.Value,
            notification.Reason);
    }
}
