using Dialysis.Contracts.Events;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alerting;

/// <summary>
/// Handles ObservationCreated events by creating alerts (stub implementation).
/// In a full implementation, this would evaluate hypotension risk and persist alerts.
/// </summary>
public sealed class ObservationCreatedAlertHandler : INotificationHandler<ObservationCreated>
{
    private readonly ILogger<ObservationCreatedAlertHandler> _logger;

    public ObservationCreatedAlertHandler(ILogger<ObservationCreatedAlertHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ObservationCreated notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ObservationCreated received: ObservationId={ObservationId}, PatientId={PatientId}, TenantId={TenantId}, LoincCode={LoincCode}, Value={Value}",
            notification.ObservationId.Value,
            notification.PatientId.Value,
            notification.TenantId.Value,
            notification.LoincCode.Value,
            notification.NumericValue);

        // Stub: In full implementation, would:
        // 1. Evaluate hypotension risk from vitals
        // 2. Create alert via IAlertStore if risk is elevated
        // 3. Publish HypotensionRiskRaised if applicable
        return Task.CompletedTask;
    }
}
