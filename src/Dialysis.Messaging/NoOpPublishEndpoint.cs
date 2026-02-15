using Transponder.Abstractions;

namespace Dialysis.Messaging;

/// <summary>
/// No-op publish endpoint used when messaging is not configured (e.g. local development without Service Bus).
/// </summary>
public sealed class NoOpPublishEndpoint : IPublishEndpoint
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => Task.CompletedTask;
}
