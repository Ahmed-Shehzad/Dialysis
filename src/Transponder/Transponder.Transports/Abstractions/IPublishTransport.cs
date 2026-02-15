namespace Transponder.Transports.Abstractions;

/// <summary>
/// Publishes transport messages to all interested subscribers.
/// </summary>
public interface IPublishTransport
{
    /// <summary>
    /// Publishes the message to the transport.
    /// </summary>
    /// <param name="message">The transport message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default);
}
