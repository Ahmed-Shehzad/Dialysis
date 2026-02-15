namespace Transponder.Transports.Abstractions;

/// <summary>
/// Provides access to a received transport message and its metadata.
/// </summary>
public interface IReceiveContext
{
    /// <summary>
    /// Gets the received transport message.
    /// </summary>
    ITransportMessage Message { get; }

    /// <summary>
    /// Gets the address of the message source, if available.
    /// </summary>
    Uri? SourceAddress { get; }

    /// <summary>
    /// Gets the address where the message was received.
    /// </summary>
    Uri? DestinationAddress { get; }

    /// <summary>
    /// Gets the cancellation token for the receive operation.
    /// </summary>
    CancellationToken CancellationToken { get; }
}
