using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// In-memory scheduled message store for testing and development.
/// </summary>
public sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
{
    private readonly Lock _sync = new();
    private readonly Dictionary<Ulid, ScheduledMessage> _messages = new();

    public Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var stored = ScheduledMessage.FromMessage(message);

        lock (_sync) _messages[stored.TokenId] = stored;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IScheduledMessage>> GetDueAsync(
        DateTimeOffset now,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxCount <= 0) return Task.FromResult<IReadOnlyList<IScheduledMessage>>(Array.Empty<IScheduledMessage>());

        List<IScheduledMessage> results;

        lock (_sync)
            results = [.. _messages.Values
                .Where(message => message.DispatchedTime is null && message.ScheduledTime <= now)
                .OrderBy(message => message.ScheduledTime)
                .Take(maxCount)
                .Cast<IScheduledMessage>()];

        return Task.FromResult<IReadOnlyList<IScheduledMessage>>(results);
    }

    public Task MarkDispatchedAsync(
        Ulid tokenId,
        DateTimeOffset dispatchedTime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
            if (_messages.TryGetValue(tokenId, out ScheduledMessage? message))
                message.MarkDispatched(dispatchedTime);

        return Task.CompletedTask;
    }

    public Task<bool> CancelAsync(Ulid tokenId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync) return Task.FromResult(_messages.Remove(tokenId));
    }

    public Task<IScheduledMessage?> GetAsync(Ulid tokenId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _ = _messages.TryGetValue(tokenId, out ScheduledMessage? message);
            return Task.FromResult<IScheduledMessage?>(message);
        }
    }
}
