using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

internal sealed class AzureServiceBusReceiveContext : IReceiveContext
{
    public AzureServiceBusReceiveContext(
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
