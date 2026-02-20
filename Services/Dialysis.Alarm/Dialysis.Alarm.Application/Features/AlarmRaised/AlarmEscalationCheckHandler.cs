using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Domain.Services;
using Dialysis.Alarm.Application.Events;

using Microsoft.Extensions.Logging;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

internal sealed class AlarmEscalationCheckHandler : IDomainEventHandler<AlarmRaisedEvent>
{
    private static readonly TimeSpan EscalationWindow = TimeSpan.FromMinutes(5);

    private readonly IIntegrationEventBuffer _buffer;
    private readonly IAlarmRepository _repository;
    private readonly AlarmEscalationService _escalationService;
    private readonly ILogger<AlarmEscalationCheckHandler> _logger;
    private readonly ITenantContext _tenant;

    public AlarmEscalationCheckHandler(
        IIntegrationEventBuffer buffer,
        IAlarmRepository repository,
        AlarmEscalationService escalationService,
        ILogger<AlarmEscalationCheckHandler> logger,
        ITenantContext tenant)
    {
        _buffer = buffer;
        _repository = repository;
        _escalationService = escalationService;
        _logger = logger;
        _tenant = tenant;
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

            _buffer.Add(new AlarmEscalationTriggeredEvent(
                notification.DeviceId?.Value,
                notification.SessionId,
                result.ActiveAlarmCount,
                result.Reason ?? string.Empty,
                _tenant.TenantId));
        }
    }
}
