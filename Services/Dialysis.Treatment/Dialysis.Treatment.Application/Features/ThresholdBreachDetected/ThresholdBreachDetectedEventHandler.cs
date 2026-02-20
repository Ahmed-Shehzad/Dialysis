using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Domain.Events;
using Dialysis.Treatment.Application.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.ThresholdBreachDetected;

internal sealed class ThresholdBreachDetectedEventHandler : IDomainEventHandler<ThresholdBreachDetectedEvent>
{
    private readonly IIntegrationEventBuffer _buffer;
    private readonly ILogger<ThresholdBreachDetectedEventHandler> _logger;
    private readonly ITenantContext _tenant;

    public ThresholdBreachDetectedEventHandler(
        IIntegrationEventBuffer buffer,
        ILogger<ThresholdBreachDetectedEventHandler> logger,
        ITenantContext tenant)
    {
        _buffer = buffer;
        _logger = logger;
        _tenant = tenant;
    }

    public Task HandleAsync(ThresholdBreachDetectedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "DomainEvent: ThresholdBreachDetected SessionId={SessionId} ObservationId={ObservationId} Code={Code} BreachType={BreachType} Value={Value} Threshold={Threshold} Direction={Direction}",
            notification.SessionId,
            notification.ObservationId,
            notification.Code.Value,
            notification.Breach.BreachType,
            notification.Breach.ObservedValue,
            notification.Breach.ThresholdValue,
            notification.Breach.Direction);

        _buffer.Add(new ThresholdBreachDetectedIntegrationEvent(
            notification.TreatmentSessionId,
            notification.SessionId,
            notification.DeviceId,
            notification.ObservationId,
            notification.Code.Value,
            notification.Breach.BreachType.ToString(),
            notification.Breach.ObservedValue,
            notification.Breach.ThresholdValue,
            notification.Breach.Direction.ToString(),
            _tenant.TenantId));

        return Task.CompletedTask;
    }
}
