using Transponder.Persistence.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Provides saga steps to be executed automatically by the pipeline.
/// </summary>
public interface ISagaStepProvider<TState, in TMessage>
    where TState : class, ISagaState
    where TMessage : class, IMessage
{
    IEnumerable<SagaStep<TState>> GetSteps(ISagaConsumeContext<TState, TMessage> context);
}
