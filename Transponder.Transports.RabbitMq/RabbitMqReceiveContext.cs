using Transponder.Transports.Abstractions;

namespace Transponder.Transports.RabbitMq;

internal sealed class RabbitMqReceiveContext : IReceiveContext
{
    public RabbitMqReceiveContext(
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
