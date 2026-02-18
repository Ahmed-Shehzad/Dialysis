using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Domain.Events;
using Dialysis.Treatment.Application.Events;

using Transponder.Abstractions;

namespace Dialysis.Treatment.Application.Features.ObservationRecorded;

internal sealed class ObservationRecordedIntegrationEventHandler : IDomainEventHandler<ObservationRecordedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITenantContext _tenant;

    public ObservationRecordedIntegrationEventHandler(IPublishEndpoint publishEndpoint, ITenantContext tenant)
    {
        _publishEndpoint = publishEndpoint;
        _tenant = tenant;
    }

    public async Task HandleAsync(ObservationRecordedEvent notification, CancellationToken cancellationToken = default)
    {
        var integrationEvent = new ObservationRecordedIntegrationEvent(
            notification.TreatmentSessionId,
            notification.SessionId,
            notification.ObservationId,
            notification.Code,
            notification.Value,
            notification.Unit,
            notification.SubId,
            notification.ChannelName,
            _tenant.TenantId);

        await _publishEndpoint.PublishAsync(integrationEvent, cancellationToken);
    }
}
