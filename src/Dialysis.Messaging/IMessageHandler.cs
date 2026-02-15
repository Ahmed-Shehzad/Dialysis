using Transponder.Abstractions;

namespace Dialysis.Messaging;

/// <summary>
/// Handles messages received via Transponder from Azure Service Bus.
/// Use with <see cref="ServiceCollectionExtensions.AddMessageConsumer{TMessage}"/>.
/// </summary>
public interface IMessageHandler<in TMessage>
    where TMessage : class, IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
