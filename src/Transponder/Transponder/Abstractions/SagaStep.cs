using Transponder.Persistence.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Represents a saga step and its compensation.
/// </summary>
public sealed class SagaStep<TState>
    where TState : class, ISagaState
{
    public SagaStep(
        Func<TState, CancellationToken, Task> executeAsync,
        Func<TState, CancellationToken, Task> compensateAsync)
    {
        ExecuteAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        CompensateAsync = compensateAsync ?? throw new ArgumentNullException(nameof(compensateAsync));
    }

    public Func<TState, CancellationToken, Task> ExecuteAsync { get; }

    public Func<TState, CancellationToken, Task> CompensateAsync { get; }
}
