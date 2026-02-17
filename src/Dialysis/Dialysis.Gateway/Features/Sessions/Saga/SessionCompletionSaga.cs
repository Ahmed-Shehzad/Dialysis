using Dialysis.Gateway.Features.Audit;
using Dialysis.Gateway.Features.Outbound.PushToEhr;
using Dialysis.Gateway.Infrastructure;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Orchestrates session completion: EHR push, audit, event export.
/// Provides compensation on failure.
/// </summary>
public sealed class SessionCompletionSaga : ISagaMessageHandler<SessionCompletionState, SessionCompletionSagaRequest>,
    ISagaStepProvider<SessionCompletionState, SessionCompletionSagaRequest>
{
    private readonly ISender _sender;
    private readonly EhrOutboundOptions _ehrOptions;
    private readonly ILogger<SessionCompletionSaga> _logger;

    public SessionCompletionSaga(
        ISender sender,
        IOptions<EhrOutboundOptions> ehrOptions,
        ILogger<SessionCompletionSaga> logger)
    {
        _sender = sender;
        _ehrOptions = ehrOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Called after steps complete successfully. State is already populated; no further action needed.
    /// </summary>
    public Task HandleAsync(ISagaConsumeContext<SessionCompletionState, SessionCompletionSagaRequest> context)
    {
        context.Saga.SessionId = context.Message.SessionId;
        context.Saga.PatientId = context.Message.PatientId;
        context.Saga.TenantId = context.Message.TenantId;
        return Task.CompletedTask;
    }

    public IEnumerable<SagaStep<SessionCompletionState>> GetSteps(
        ISagaConsumeContext<SessionCompletionState, SessionCompletionSagaRequest> context)
    {
        yield return new SagaStep<SessionCompletionState>(
            executeAsync: async (state, ct) =>
            {
                state.SessionId = context.Message.SessionId;
                state.PatientId = context.Message.PatientId;
                state.TenantId = context.Message.TenantId;
                SagaTenantScope.Set(new TenantId(state.TenantId));
                try
                {
                    await PushToEhrAsync(state, ct);
                }
                finally
                {
                    SagaTenantScope.Clear();
                }
            },
            compensateAsync: CompensateEhrPushAsync);
        yield return new SagaStep<SessionCompletionState>(
            executeAsync: async (state, ct) =>
            {
                SagaTenantScope.Set(new TenantId(state.TenantId));
                try
                {
                    await RecordAuditAsync(state, ct);
                }
                finally
                {
                    SagaTenantScope.Clear();
                }
            },
            compensateAsync: CompensateAuditAsync);
    }

    private async Task PushToEhrAsync(SessionCompletionState state, CancellationToken ct)
    {
        var baseUrl = _ehrOptions.PdmsFhirBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogDebug(
                "SessionCompletionSaga: PdmsFhirBaseUrl not configured. Skipping EHR push for SessionId={SessionId}.",
                state.SessionId);
            state.EhrPushSucceeded = true; // skip counts as success
            return;
        }

        var baseUrlWithSlash = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        var command = new PushToEhrCommand(baseUrlWithSlash, state.PatientId);
        var result = await _sender.SendAsync(command, ct);

        state.EhrPushSucceeded = result.Success;
        if (result.Success)
        {
            _logger.LogInformation(
                "SessionCompletionSaga: EHR push succeeded for SessionId={SessionId}, PatientId={PatientId}",
                state.SessionId,
                state.PatientId);
        }
        else
        {
            _logger.LogWarning(
                "SessionCompletionSaga: EHR push failed for SessionId={SessionId}, PatientId={PatientId}, Error={Error}",
                state.SessionId,
                state.PatientId,
                result.ErrorMessage);
            throw new InvalidOperationException($"EHR push failed: {result.ErrorMessage}");
        }
    }

    private Task CompensateEhrPushAsync(SessionCompletionState state, CancellationToken ct)
    {
        _logger.LogWarning(
            "SessionCompletionSaga: Compensating EHR push for SessionId={SessionId}. Manual reconciliation may be required.",
            state.SessionId);
        return Task.CompletedTask;
    }

    private async Task RecordAuditAsync(SessionCompletionState state, CancellationToken ct)
    {
        var command = new RecordAuditCommand(
            Action: "SessionCompleted",
            ResourceType: "Session",
            Actor: "api",
            ResourceId: state.SessionId,
            PatientId: state.PatientId,
            Details: null);

        var result = await _sender.SendAsync(command, ct);
        state.AuditRecorded = result.Error is null;

        if (result.Error is not null)
        {
            _logger.LogWarning(
                "SessionCompletionSaga: Audit failed for SessionId={SessionId}, Error={Error}",
                state.SessionId,
                result.Error);
            throw new InvalidOperationException($"Audit failed: {result.Error}");
        }
    }

    private Task CompensateAuditAsync(SessionCompletionState state, CancellationToken ct)
    {
        _logger.LogWarning(
            "SessionCompletionSaga: Compensating audit for SessionId={SessionId}. Manual audit entry may be required.",
            state.SessionId);
        return Task.CompletedTask;
    }
}
