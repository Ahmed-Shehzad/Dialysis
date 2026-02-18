namespace Transponder.Transports.Abstractions;

/// <summary>
/// Sends transport messages to a specific destination.
/// </summary>
public interface ISendTransport
{
    /// <summary>
    /// Sends the message to the destination.
    /// </summary>
    /// <param name="message">The transport message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default);
}
