using Transponder.Persistence.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Handles messages for a saga.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface ISagaMessageHandler<TState, in TMessage>
    where TState : class, ISagaState
    where TMessage : class, IMessage
{
    /// <summary>
    /// Handles the message and updates the saga state.
    /// </summary>
    /// <param name="context">The saga consume context.</param>
    Task HandleAsync(ISagaConsumeContext<TState, TMessage> context);
}
