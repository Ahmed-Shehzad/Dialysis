namespace Transponder.Abstractions;

/// <summary>
/// Provides metadata and utilities for handling a consumed message.
/// </summary>
public interface IConsumeContext : IPublishEndpoint, ISendEndpointProvider
{
    /// <summary>
    /// Gets the unique message identifier if available.
    /// </summary>
    Ulid? MessageId { get; }

    /// <summary>
    /// Gets the correlation identifier used to relate messages.
    /// </summary>
    Ulid? CorrelationId { get; }

    /// <summary>
    /// Gets the conversation identifier for a logical flow of messages.
    /// </summary>
    Ulid? ConversationId { get; }

    /// <summary>
    /// Gets the address of the message source.
    /// </summary>
    Uri? SourceAddress { get; }

    /// <summary>
    /// Gets the address where the message was received.
    /// </summary>
    Uri? DestinationAddress { get; }

    /// <summary>
    /// Gets the message sent timestamp, if available.
    /// </summary>
    DateTimeOffset? SentTime { get; }

    /// <summary>
    /// Gets the headers associated with the message.
    /// </summary>
    IReadOnlyDictionary<string, object?> Headers { get; }

    /// <summary>
    /// Gets the cancellation token associated with the consume operation.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Responds to the sender with the specified message.
    /// </summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task RespondAsync<TResponse>(TResponse response, CancellationToken cancellationToken = default)
        where TResponse : class, IMessage;
}

/// <summary>
/// Provides access to the consumed message and its metadata.
/// </summary>
/// <typeparam name="TMessage">The type of the consumed message.</typeparam>
public interface IConsumeContext<out TMessage> : IConsumeContext
    where TMessage : class, IMessage
{
    /// <summary>
    /// Gets the consumed message instance.
    /// </summary>
    TMessage Message { get; }
}
