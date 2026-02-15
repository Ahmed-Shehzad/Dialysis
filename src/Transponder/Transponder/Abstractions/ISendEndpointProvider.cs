namespace Transponder.Abstractions;

/// <summary>
/// Provides send endpoints by address.
/// </summary>
public interface ISendEndpointProvider
{
    /// <summary>
    /// Gets a send endpoint for the specified address.
    /// </summary>
    /// <param name="address">The endpoint address.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default);
}
