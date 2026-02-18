using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Receive context for SSE transport handling.
/// </summary>
internal sealed class SseReceiveContext : IReceiveContext
{
    public SseReceiveContext(
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

    public ITransportMessage Message { get; }

    public Uri? SourceAddress { get; }

    public Uri? DestinationAddress { get; }

    public CancellationToken CancellationToken { get; }
}
