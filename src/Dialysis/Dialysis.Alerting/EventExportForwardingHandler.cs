using Dialysis.Contracts.Events;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alerting;

/// <summary>
/// Forwards ObservationCreated to IEventExportPublisher for analytics/ETL. Phase 2.3.2.
/// </summary>
public sealed class EventExportForwardingHandler : INotificationHandler<ObservationCreated>
{
    private readonly IEventExportPublisher _export;
    private readonly ILogger<EventExportForwardingHandler> _logger;

    public EventExportForwardingHandler(IEventExportPublisher export, ILogger<EventExportForwardingHandler> logger)
    {
        _export = export;
        _logger = logger;
    }

    public async Task HandleAsync(ObservationCreated notification, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            EventType = "ObservationCreated",
            ObservationId = notification.ObservationId.Value,
            PatientId = notification.PatientId.Value,
            TenantId = notification.TenantId.Value,
            LoincCode = notification.LoincCode.Value,
            NumericValue = notification.NumericValue,
            Effective = notification.Effective.Value.ToString("O")
        };
        await _export.PublishAsync("ObservationCreated", payload, cancellationToken);
        _logger.LogDebug("Forwarded ObservationCreated to event export");
    }
}
