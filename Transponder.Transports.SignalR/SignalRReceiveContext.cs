using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// Receive context for SignalR transport handling.
/// </summary>
internal sealed class SignalRReceiveContext : IReceiveContext
{
    public SignalRReceiveContext(
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
