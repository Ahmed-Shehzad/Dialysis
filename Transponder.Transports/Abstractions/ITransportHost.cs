
namespace Transponder.Transports.Abstractions;

/// <summary>
/// Represents a transport host that manages endpoints and transport lifecycle.
/// </summary>
public interface ITransportHost : ISendTransportProvider, IPublishTransportProvider, IAsyncDisposable
{
    /// <summary>
    /// Gets the host address.
    /// </summary>
    Uri Address { get; }

    /// <summary>
    /// Starts the transport host.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the transport host.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects a receive endpoint to the transport host.
    /// </summary>
    /// <param name="configuration">The receive endpoint configuration.</param>
    IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration);
}
