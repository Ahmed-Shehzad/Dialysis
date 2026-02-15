using Transponder.Persistence;
using Transponder.Transports.Abstractions;

namespace Transponder;

internal static class TransponderMessageContextFactory
{
    public static TransponderMessageContext FromTransportMessage(
        ITransportMessage message,
        Uri? sourceAddress,
        Uri? destinationAddress)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new TransponderMessageContext(
            message.MessageId,
            message.CorrelationId,
            message.ConversationId,
            message.MessageType,
            sourceAddress,
            destinationAddress,
            message.SentTime,
            message.Headers);
    }

    public static TransponderMessageContext FromOutboxMessage(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new TransponderMessageContext(
            message.MessageId,
            message.CorrelationId,
            message.ConversationId,
            message.MessageType,
            message.SourceAddress,
            message.DestinationAddress,
            message.SentTime,
            message.Headers);
    }
}
