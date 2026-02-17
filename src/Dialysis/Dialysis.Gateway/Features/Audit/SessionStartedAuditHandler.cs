using Dialysis.Contracts.Events;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Audit;

/// <summary>
/// C5: Records audit when a session is started.
/// </summary>
public sealed class SessionStartedAuditHandler : INotificationHandler<SessionStarted>
{
    private readonly ISender _sender;
    private readonly ILogger<SessionStartedAuditHandler> _logger;

    public SessionStartedAuditHandler(ISender sender, ILogger<SessionStartedAuditHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(SessionStarted notification, CancellationToken cancellationToken = default)
    {
        var command = new RecordAuditCommand(
            Action: "SessionStarted",
            ResourceType: "Session",
            Actor: "api",
            ResourceId: notification.SessionId,
            PatientId: notification.PatientId.Value,
            Details: null);

        var result = await _sender.SendAsync(command, cancellationToken);
        if (result.Error is not null)
            _logger.LogWarning("SessionStartedAuditHandler: Audit failed for SessionId={SessionId}, Error={Error}", notification.SessionId, result.Error);
    }
}
