namespace Transponder.Abstractions;

/// <summary>
/// Defines a consumer that handles messages of a specific type.
/// </summary>
/// <typeparam name="TMessage">The type of message to consume.</typeparam>
public interface IConsumer<in TMessage> where TMessage : class, IMessage
{
    /// <summary>
    /// Consumes the incoming message.
    /// </summary>
    /// <param name="context">The consume context containing the message and metadata.</param>
    Task ConsumeAsync(IConsumeContext<TMessage> context);
}
