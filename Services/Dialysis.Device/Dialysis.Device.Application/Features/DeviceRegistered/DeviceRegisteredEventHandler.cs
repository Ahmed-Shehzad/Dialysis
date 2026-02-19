using BuildingBlocks.Abstractions;

using Dialysis.Device.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Device.Application.Features.DeviceRegistered;

internal sealed class DeviceRegisteredEventHandler : IDomainEventHandler<DeviceRegisteredEvent>
{
    private readonly ILogger<DeviceRegisteredEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public DeviceRegisteredEventHandler(ILogger<DeviceRegisteredEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(DeviceRegisteredEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: DeviceRegistered DeviceId={DeviceId} EUI64={DeviceEui64} Manufacturer={Manufacturer} Model={Model}",
            notification.DeviceId,
            notification.DeviceEui64,
            notification.Manufacturer,
            notification.Model);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create,
            "Device",
            notification.DeviceId.ToString(),
            null,
            AuditOutcome.Success,
            "Device registered (domain event)",
            null),
            cancellationToken);
    }
}
