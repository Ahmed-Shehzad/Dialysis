using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Events;

using Transponder.Abstractions;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

internal sealed class AlarmRaisedIntegrationEventHandler : IDomainEventHandler<AlarmRaisedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITenantContext _tenant;

    public AlarmRaisedIntegrationEventHandler(IPublishEndpoint publishEndpoint, ITenantContext tenant)
    {
        _publishEndpoint = publishEndpoint;
        _tenant = tenant;
    }

    public async Task HandleAsync(AlarmRaisedEvent notification, CancellationToken cancellationToken = default)
    {
        var integrationEvent = new AlarmRaisedIntegrationEvent(
            notification.AlarmId,
            notification.AlarmType,
            notification.EventPhase,
            notification.AlarmState,
            notification.DeviceId?.Value,
            notification.SessionId,
            notification.OccurredAt,
            _tenant.TenantId);

        await _publishEndpoint.PublishAsync(integrationEvent, cancellationToken);
    }
}
