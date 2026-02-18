using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Features.AlarmAcknowledged;

internal sealed class AlarmAcknowledgedEventHandler : IDomainEventHandler<AlarmAcknowledgedEvent>
{
    private readonly ILogger<AlarmAcknowledgedEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public AlarmAcknowledgedEventHandler(ILogger<AlarmAcknowledgedEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(AlarmAcknowledgedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: AlarmAcknowledged AlarmId={AlarmId}",
            notification.AlarmId);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Update,
            "Alarm",
            notification.AlarmId.ToString(),
            null,
            AuditOutcome.Success,
            "Alarm acknowledged",
            null),
            cancellationToken);
    }
}
