using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Domain.Services;

using Microsoft.Extensions.Logging;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

internal sealed class AlarmEscalationCheckHandler : IDomainEventHandler<AlarmRaisedEvent>
{
    private static readonly TimeSpan EscalationWindow = TimeSpan.FromMinutes(5);

    private readonly IAlarmRepository _repository;
    private readonly AlarmEscalationService _escalationService;
    private readonly ILogger<AlarmEscalationCheckHandler> _logger;

    public AlarmEscalationCheckHandler(
        IAlarmRepository repository,
        AlarmEscalationService escalationService,
        ILogger<AlarmEscalationCheckHandler> logger)
    {
        _repository = repository;
        _escalationService = escalationService;
        _logger = logger;
    }

    public async Task HandleAsync(AlarmRaisedEvent notification, CancellationToken cancellationToken = default)
    {
        SessionId? sessionId = string.IsNullOrEmpty(notification.SessionId) ? null : new SessionId(notification.SessionId);

        IReadOnlyList<Domain.Alarm> recentAlarms = await _repository.GetRecentActiveAlarmsForEscalationAsync(
            notification.DeviceId, sessionId, EscalationWindow, notification.AlarmId, cancellationToken);

        EscalationResult result = _escalationService.Evaluate(recentAlarms, notification.DeviceId);

        if (result.ShouldEscalate)
        {
            _logger.LogWarning(
                "Alarm escalation: DeviceId={DeviceId} SessionId={SessionId} ActiveCount={Count} Reason={Reason}",
                notification.DeviceId?.Value,
                notification.SessionId,
                result.ActiveAlarmCount,
                result.Reason);
        }
    }
}
