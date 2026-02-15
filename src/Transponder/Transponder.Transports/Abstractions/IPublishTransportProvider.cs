namespace Transponder.Transports.Abstractions;

/// <summary>
/// Provides publish transports for message types.
/// </summary>
public interface IPublishTransportProvider
{
    /// <summary>
    /// Gets a publish transport for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IPublishTransport> GetPublishTransportAsync(Type messageType, CancellationToken cancellationToken = default);
}
