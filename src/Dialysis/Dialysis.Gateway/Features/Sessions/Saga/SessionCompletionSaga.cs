using Dialysis.Gateway.Features.Sessions.Saga.Steps;
using Dialysis.SharedKernel.ValueObjects;

using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Single responsibility: orchestrate session completion steps.
/// Composes step executors; does not implement step logic.
/// </summary>
public sealed class SessionCompletionSaga : ISagaMessageHandler<SessionCompletionState, SessionCompletionSagaRequest>,
    ISagaStepProvider<SessionCompletionState, SessionCompletionSagaRequest>
{
    private readonly ISessionCompletionStep _ehrPushStep;
    private readonly ISessionCompletionStep _auditStep;
    private readonly ISessionCompletionStep _eventExportStep;

    public SessionCompletionSaga(
        SessionCompletionEhrPushStep ehrPushStep,
        SessionCompletionAuditStep auditStep,
        SessionCompletionEventExportStep eventExportStep)
    {
        _ehrPushStep = ehrPushStep;
        _auditStep = auditStep;
        _eventExportStep = eventExportStep;
    }

    /// <summary>
    /// Populates state from message. Called before steps run.
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
        yield return WrapStep(context, _ehrPushStep);
        yield return WrapStep(context, _auditStep);
        yield return WrapStep(context, _eventExportStep);
    }

    private static SagaStep<SessionCompletionState> WrapStep(
        ISagaConsumeContext<SessionCompletionState, SessionCompletionSagaRequest> context,
        ISessionCompletionStep step)
    {
        return new SagaStep<SessionCompletionState>(
            executeAsync: async (state, ct) =>
            {
                SagaTenantScope.Set(new TenantId(state.TenantId));
                try
                {
                    await step.ExecuteAsync(state, ct);
                }
                finally
                {
                    SagaTenantScope.Clear();
                }
            },
            compensateAsync: static (_, _) => Task.CompletedTask);
    }
}
