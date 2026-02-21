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

        var entity = OutboxMessageRecord.FromMessage(message);
        _ = await _context.Set<OutboxMessageRecord>()
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0) return [];

        List<OutboxMessageRecord> messages = await _context.Set<OutboxMessageRecord>()
            .AsNoTracking()
            .Where(message => message.SentTime == null)
            .OrderBy(message => message.EnqueuedTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. messages.Select(static IOutboxMessage (message) => message)];
    }

    /// <inheritdoc />
    public async Task MarkSentAsync(
        Ulid messageId,
        DateTimeOffset sentTime,
        CancellationToken cancellationToken = default)
    {
        OutboxMessageRecord? message = await _context.Set<OutboxMessageRecord>()
            .FirstOrDefaultAsync(entity => entity.MessageId == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message == null) return;

        message.SentTime = sentTime;
    }
}
