using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Features.AlarmCleared;

internal sealed class AlarmClearedEventHandler : IDomainEventHandler<AlarmClearedEvent>
{
    private readonly ILogger<AlarmClearedEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public AlarmClearedEventHandler(ILogger<AlarmClearedEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(AlarmClearedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: AlarmCleared AlarmId={AlarmId}",
            notification.AlarmId);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Update,
            "Alarm",
            notification.AlarmId.ToString(),
            null,
            AuditOutcome.Success,
            "Alarm cleared",
            null),
            cancellationToken);
    }
}
