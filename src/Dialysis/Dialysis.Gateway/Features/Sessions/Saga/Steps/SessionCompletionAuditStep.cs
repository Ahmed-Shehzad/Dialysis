using Dialysis.Gateway.Features.Audit;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Sessions.Saga.Steps;

/// <summary>
/// Single responsibility: record audit for session completion.
/// </summary>
public sealed class SessionCompletionAuditStep : ISessionCompletionStep
{
    private readonly ISender _sender;
    private readonly ILogger<SessionCompletionAuditStep> _logger;

    public SessionCompletionAuditStep(
        ISender sender,
        ILogger<SessionCompletionAuditStep> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task ExecuteAsync(SessionCompletionState state, CancellationToken cancellationToken = default)
    {
        var command = new RecordAuditCommand(
            Action: "SessionCompleted",
            ResourceType: "Session",
            Actor: "api",
            ResourceId: state.SessionId,
            PatientId: state.PatientId,
            Details: null);

        var result = await _sender.SendAsync(command, cancellationToken);
        state.AuditRecorded = result.Error is null;

        if (result.Error is not null)
        {
            _logger.LogWarning(
                "SessionCompletionAuditStep: Audit failed for SessionId={SessionId}, Error={Error}",
                state.SessionId,
                result.Error);
            throw new InvalidOperationException($"Audit failed: {result.Error}");
        }
    }
}
