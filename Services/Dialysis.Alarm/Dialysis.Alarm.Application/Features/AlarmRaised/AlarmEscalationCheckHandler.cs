using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Domain.Events;
using Dialysis.Alarm.Application.Domain.Services;

using Microsoft.Extensions.Logging;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

/// <summary>
/// When escalation is triggered, creates EscalationIncident aggregate which raises AlarmEscalationTriggeredEvent.
/// </summary>
internal sealed class AlarmEscalationCheckHandler : IDomainEventHandler<AlarmEscalationCheckEvent>
{
    private static readonly TimeSpan EscalationWindow = TimeSpan.FromMinutes(5);

    private readonly IEscalationIncidentStore _escalationStore;
    private readonly IAlarmRepository _repository;
    private readonly AlarmEscalationService _escalationService;
    private readonly ILogger<AlarmEscalationCheckHandler> _logger;
    private readonly ITenantContext _tenant;

    public AlarmEscalationCheckHandler(
        IEscalationIncidentStore escalationStore,
        IAlarmRepository repository,
        AlarmEscalationService escalationService,
        ILogger<AlarmEscalationCheckHandler> logger,
        ITenantContext tenant)
    {
        _escalationStore = escalationStore ?? throw new ArgumentNullException(nameof(escalationStore));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _escalationService = escalationService ?? throw new ArgumentNullException(nameof(escalationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    public async Task HandleAsync(AlarmEscalationCheckEvent notification, CancellationToken cancellationToken = default)
    {
        SessionId? sessionId = notification.SessionId;

        IReadOnlyList<Domain.Alarm> recentAlarms = await _repository.GetRecentActiveAlarmsForEscalationAsync(
            notification.DeviceId, sessionId, EscalationWindow, notification.AlarmId, cancellationToken);

        EscalationResult result = _escalationService.Evaluate(recentAlarms, notification.DeviceId);

        if (result.ShouldEscalate)
        {
            _logger.LogWarning(
                "Alarm escalation: DeviceId={DeviceId} SessionId={SessionId} ActiveCount={Count} Reason={Reason}",
                notification.DeviceId?.Value,
                notification.SessionId?.Value,
                result.ActiveAlarmCount,
                result.Reason);

            var incident = EscalationIncident.Record(
                notification.DeviceId?.Value,
                notification.SessionId?.Value ?? string.Empty,
                result.ActiveAlarmCount,
                result.Reason ?? string.Empty,
                _tenant.TenantId);

            _escalationStore.Add(incident);
        }
    }
}
