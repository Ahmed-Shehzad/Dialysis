using Dialysis.Contracts.Events;
using Dialysis.Gateway.Features.Audit;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Sessions;

/// <summary>
/// Records an audit entry when a session is completed.
/// </summary>
public sealed class SessionCompletedAuditHandler : INotificationHandler<SessionCompleted>
{
    private readonly ISender _sender;
    private readonly ILogger<SessionCompletedAuditHandler> _logger;

    public SessionCompletedAuditHandler(ISender sender, ILogger<SessionCompletedAuditHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(SessionCompleted notification, CancellationToken cancellationToken = default)
    {
        var command = new RecordAuditCommand(
            Action: "SessionCompleted",
            ResourceType: "Session",
            Actor: "api",
            ResourceId: notification.SessionId,
            PatientId: notification.PatientId.Value,
            Details: null);

        var result = await _sender.SendAsync(command, cancellationToken);

        if (result.Error is not null)
        {
            _logger.LogWarning(
                "SessionCompletedAuditHandler: Audit recording failed for SessionId={SessionId}, Error={Error}",
                notification.SessionId,
                result.Error);
            return;
        }

        _logger.LogDebug(
            "SessionCompletedAuditHandler: Audit recorded for SessionId={SessionId}, PatientId={PatientId}",
            notification.SessionId,
            notification.PatientId.Value);
    }
}
