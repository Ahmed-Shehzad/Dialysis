using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// Default inbox state implementation.
/// </summary>
public sealed class InboxState : IInboxState
{
    public InboxState(
        Ulid messageId,
        string consumerId,
        DateTimeOffset? receivedTime = null,
        DateTimeOffset? processedTime = null)
    {
        if (messageId == Ulid.Empty) throw new ArgumentException("MessageId must be provided.", nameof(messageId));

        if (string.IsNullOrWhiteSpace(consumerId)) throw new ArgumentException("ConsumerId must be provided.", nameof(consumerId));

        MessageId = messageId;
        ConsumerId = consumerId;
        ReceivedTime = receivedTime ?? DateTimeOffset.UtcNow;
        ProcessedTime = processedTime;
    }

    /// <inheritdoc />
    public Ulid MessageId { get; }

    /// <inheritdoc />
    public string ConsumerId { get; }

    /// <inheritdoc />
    public DateTimeOffset ReceivedTime { get; }

    /// <inheritdoc />
    public DateTimeOffset? ProcessedTime { get; private set; }

    internal void MarkProcessed(DateTimeOffset processedTime) => ProcessedTime = processedTime;

    public static InboxState FromState(IInboxState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new InboxState(
            state.MessageId,
            state.ConsumerId,
            state.ReceivedTime,
            state.ProcessedTime);
    }
}
