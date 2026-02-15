namespace Transponder.Abstractions;

/// <summary>
/// Defines the ability to publish messages to all interested subscribers.
/// </summary>
public interface IPublishEndpoint
{
    /// <summary>
    /// Publishes a message to all configured subscribers.
    /// </summary>
    /// <typeparam name="TMessage">The message type to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;
}
