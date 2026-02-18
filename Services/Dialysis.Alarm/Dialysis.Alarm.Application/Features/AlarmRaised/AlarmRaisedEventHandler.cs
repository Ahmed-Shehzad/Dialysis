using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

internal sealed class AlarmRaisedEventHandler : IDomainEventHandler<AlarmRaisedEvent>
{
    private readonly ILogger<AlarmRaisedEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public AlarmRaisedEventHandler(ILogger<AlarmRaisedEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(AlarmRaisedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: AlarmRaised AlarmId={AlarmId} Type={AlarmType} Phase={Phase} State={State} DeviceId={DeviceId} SessionId={SessionId}",
            notification.AlarmId,
            notification.AlarmType,
            notification.EventPhase.Value,
            notification.AlarmState.Value,
            notification.DeviceId?.Value,
            notification.SessionId);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create,
            "Alarm",
            notification.AlarmId.ToString(),
            null,
            AuditOutcome.Success,
            $"Alarm raised: {notification.AlarmType} ({notification.EventPhase.Value})",
            null),
            cancellationToken);
    }
}
