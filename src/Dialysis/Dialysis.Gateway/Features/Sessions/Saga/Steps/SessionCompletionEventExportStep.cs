using Dialysis.SharedKernel.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Sessions.Saga.Steps;

/// <summary>
/// Single responsibility: publish session completion to event export (ASB) for downstream consumers.
/// Requires EventExport (ASB) to be configured; uses IEventExportPublisher.
/// </summary>
public sealed class SessionCompletionEventExportStep : ISessionCompletionStep
{
    private readonly IEventExportPublisher _eventExport;
    private readonly ILogger<SessionCompletionEventExportStep> _logger;

    public SessionCompletionEventExportStep(
        IEventExportPublisher eventExport,
        ILogger<SessionCompletionEventExportStep> logger)
    {
        _eventExport = eventExport;
        _logger = logger;
    }

    public async Task ExecuteAsync(SessionCompletionState state, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            EventType = "SessionCompleted",
            SessionId = state.SessionId,
            PatientId = state.PatientId,
            TenantId = state.TenantId
        };
        await _eventExport.PublishAsync("SessionCompleted", payload, cancellationToken);
        state.EventExported = true;
        _logger.LogDebug(
            "SessionCompletionEventExportStep: Event exported for SessionId={SessionId}, PatientId={PatientId}",
            state.SessionId,
            state.PatientId);
    }
}
