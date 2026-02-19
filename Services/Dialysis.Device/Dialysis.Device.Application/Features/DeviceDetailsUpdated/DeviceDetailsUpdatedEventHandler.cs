using BuildingBlocks.Abstractions;

using Dialysis.Device.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Device.Application.Features.DeviceDetailsUpdated;

internal sealed class DeviceDetailsUpdatedEventHandler : IDomainEventHandler<DeviceDetailsUpdatedEvent>
{
    private readonly ILogger<DeviceDetailsUpdatedEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public DeviceDetailsUpdatedEventHandler(ILogger<DeviceDetailsUpdatedEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(DeviceDetailsUpdatedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: DeviceDetailsUpdated DeviceId={DeviceId} EUI64={DeviceEui64}",
            notification.DeviceId,
            notification.DeviceEui64);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Update,
            "Device",
            notification.DeviceId.ToString(),
            null,
            AuditOutcome.Success,
            "Device details updated (domain event)",
            null),
            cancellationToken);
    }
}
