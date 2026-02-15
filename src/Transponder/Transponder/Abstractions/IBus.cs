namespace Transponder.Abstractions;

/// <summary>
/// Represents the main message bus contract for sending, publishing, requesting, and scheduling messages.
/// </summary>
public interface IBus : IPublishEndpoint, ISendEndpointProvider, IClientFactory, IMessageScheduler
{
    /// <summary>
    /// Gets the bus address.
    /// </summary>
    Uri Address { get; }
}
