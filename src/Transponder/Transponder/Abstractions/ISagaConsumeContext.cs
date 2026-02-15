using Transponder.Persistence.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Provides access to a saga state alongside the consumed message.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface ISagaConsumeContext<TState, out TMessage> : IConsumeContext<TMessage>
    where TState : class, ISagaState
    where TMessage : class, IMessage
{
    /// <summary>
    /// Gets the saga state.
    /// </summary>
    TState Saga { get; }

    /// <summary>
    /// Gets whether the saga was created for this message.
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    /// Gets the saga interaction style.
    /// </summary>
    SagaStyle Style { get; }

    /// <summary>
    /// Gets whether the saga should be completed after handling.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Executes saga steps in order and compensates in reverse on failure.
    /// </summary>
    Task<SagaStatus> ExecuteStepsAsync(
        IEnumerable<SagaStep<TState>> steps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the saga as completed after the handler finishes.
    /// </summary>
    void MarkCompleted();
}
