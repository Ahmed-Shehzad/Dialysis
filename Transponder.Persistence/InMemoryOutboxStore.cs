using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// In-memory outbox store for testing and development.
/// </summary>
public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly object _sync = new();
    private readonly Dictionary<Ulid, OutboxMessage> _messages = new();

    /// <inheritdoc />
    public Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var stored = OutboxMessage.FromMessage(message);

        lock (_sync) _messages[stored.MessageId] = stored;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxCount <= 0) return Task.FromResult<IReadOnlyList<IOutboxMessage>>(Array.Empty<IOutboxMessage>());

        List<IOutboxMessage> results;

        lock (_sync)
            results = [.. _messages.Values
                .Where(message => message.SentTime is null)
                .OrderBy(message => message.EnqueuedTime)
                .Take(maxCount)
                .Cast<IOutboxMessage>()];

        return Task.FromResult<IReadOnlyList<IOutboxMessage>>(results);
    }

    /// <inheritdoc />
    public Task MarkSentAsync(Ulid messageId, DateTimeOffset sentTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
            if (_messages.TryGetValue(messageId, out OutboxMessage? message))
                message.MarkSent(sentTime);

        return Task.CompletedTask;
    }
}
