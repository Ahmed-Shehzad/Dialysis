namespace Transponder.Abstractions;

/// <summary>
/// Controls the lifecycle of the bus.
/// </summary>
public interface IBusControl : IBus, IAsyncDisposable
{
    /// <summary>
    /// Starts the bus and all configured receive endpoints.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the bus and all configured receive endpoints.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
