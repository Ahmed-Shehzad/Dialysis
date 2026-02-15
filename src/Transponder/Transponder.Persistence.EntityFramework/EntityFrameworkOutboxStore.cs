using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework outbox store implementation.
/// </summary>
public sealed class EntityFrameworkOutboxStore : IOutboxStore
{
    private readonly DbContext _context;

    public EntityFrameworkOutboxStore(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var entity = OutboxMessageEntity.FromMessage(message);
        _ = await _context.Set<OutboxMessageEntity>()
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0) return [];

        var messages = await _context.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(message => message.SentTime == null)
            .OrderBy(message => message.EnqueuedTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. messages.Select(static message => (IOutboxMessage)message)];
    }

    /// <inheritdoc />
    public async Task MarkSentAsync(
        Ulid messageId,
        DateTimeOffset sentTime,
        CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<OutboxMessageEntity>()
            .FirstOrDefaultAsync(entity => entity.MessageId == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message == null) return;

        message.SentTime = sentTime;
    }
}
