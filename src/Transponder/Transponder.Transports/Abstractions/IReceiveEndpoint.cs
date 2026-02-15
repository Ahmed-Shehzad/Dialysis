namespace Transponder.Transports.Abstractions;

/// <summary>
/// Represents a receive endpoint that consumes messages from a transport.
/// </summary>
public interface IReceiveEndpoint : IAsyncDisposable
{
    /// <summary>
    /// Gets the input address for this endpoint.
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// Starts the receive endpoint.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the receive endpoint.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
