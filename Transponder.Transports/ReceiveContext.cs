using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Default receive context for in-memory transport handling.
/// </summary>
internal sealed class ReceiveContext : IReceiveContext
{
    public ReceiveContext(
        ITransportMessage message,
        Uri? sourceAddress,
        Uri? destinationAddress,
        CancellationToken cancellationToken)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        CancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    public ITransportMessage Message { get; }

    /// <inheritdoc />
    public Uri? SourceAddress { get; }

    /// <inheritdoc />
    public Uri? DestinationAddress { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }
}
