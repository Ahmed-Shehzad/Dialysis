using Dialysis.Contracts.Events;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Audit;

/// <summary>
/// C5: Records audit when a patient is created.
/// </summary>
public sealed class PatientCreatedAuditHandler : INotificationHandler<PatientCreated>
{
    private readonly ISender _sender;
    private readonly ILogger<PatientCreatedAuditHandler> _logger;

    public PatientCreatedAuditHandler(ISender sender, ILogger<PatientCreatedAuditHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(PatientCreated notification, CancellationToken cancellationToken = default)
    {
        var command = new RecordAuditCommand(
            Action: "PatientCreated",
            ResourceType: "Patient",
            Actor: "api",
            ResourceId: notification.LogicalId,
            PatientId: notification.LogicalId,
            Details: null);

        var result = await _sender.SendAsync(command, cancellationToken);
        if (result.Error is not null)
            _logger.LogWarning("PatientCreatedAuditHandler: Audit failed for LogicalId={LogicalId}, Error={Error}", notification.LogicalId, result.Error);
    }
}
