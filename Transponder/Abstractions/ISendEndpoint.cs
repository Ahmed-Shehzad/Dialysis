namespace Transponder.Abstractions;

/// <summary>
/// Defines the ability to send messages to a specific endpoint.
/// </summary>
public interface ISendEndpoint
{
    /// <summary>
    /// Sends a message to the endpoint.
    /// </summary>
    /// <typeparam name="TMessage">The message type to send.</typeparam>
    /// <param name="message">The message instance to send.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;
}
