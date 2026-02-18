namespace Transponder.Transports.Abstractions;

/// <summary>
/// Provides send transports for destination addresses.
/// </summary>
public interface ISendTransportProvider
{
    /// <summary>
    /// Gets a send transport for the specified address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default);
}
