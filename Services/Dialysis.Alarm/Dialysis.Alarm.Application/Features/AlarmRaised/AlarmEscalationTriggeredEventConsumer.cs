using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

/// <summary>
/// In-process consumer for AlarmEscalationTriggeredEvent. Receives events post-commit.
/// Future: nursing dashboard, FHIR DetectedIssue with escalation severity.
/// </summary>
internal sealed class AlarmEscalationTriggeredEventConsumer : IIntegrationEventHandler<AlarmEscalationTriggeredEvent>
{
    private readonly ILogger<AlarmEscalationTriggeredEventConsumer> _logger;

    public AlarmEscalationTriggeredEventConsumer(ILogger<AlarmEscalationTriggeredEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AlarmEscalationTriggeredEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "IntegrationEvent: AlarmEscalationTriggered DeviceId={DeviceId} SessionId={SessionId} ActiveCount={Count} Reason={Reason}",
            notification.DeviceId,
            notification.SessionId,
            notification.ActiveAlarmCount,
            notification.Reason);

        return Task.CompletedTask;
    }
}
