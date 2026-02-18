using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Abstractions;
using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework scheduled message store implementation.
/// </summary>
public sealed class EntityFrameworkScheduledMessageStore<TContext> : IScheduledMessageStore
    where TContext : DbContext
{
    private readonly IEntityFrameworkDbContextFactory<TContext> _contextFactory;

    public EntityFrameworkScheduledMessageStore(IEntityFrameworkDbContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using TContext context = _contextFactory.CreateDbContext();
        var entity = ScheduledMessageRecord.FromMessage(message);
        _ = await context.Set<ScheduledMessageRecord>()
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IScheduledMessage>> GetDueAsync(
        DateTimeOffset now,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0) return [];

        await using TContext context = _contextFactory.CreateDbContext();
        List<ScheduledMessageRecord> messages = await context.Set<ScheduledMessageRecord>()
            .AsNoTracking()
            .Where(message => message.DispatchedTime == null && message.ScheduledTime <= now)
            .OrderBy(message => message.ScheduledTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. messages.Select(static message => (IScheduledMessage)message)];
    }

    public async Task MarkDispatchedAsync(
        Ulid tokenId,
        DateTimeOffset dispatchedTime,
        CancellationToken cancellationToken = default)
    {
        await using TContext context = _contextFactory.CreateDbContext();
        ScheduledMessageRecord? message = await context.Set<ScheduledMessageRecord>()
            .FirstOrDefaultAsync(entity => entity.TokenId == tokenId, cancellationToken)
            .ConfigureAwait(false);

        if (message == null) return;

        message.DispatchedTime = dispatchedTime;
        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> CancelAsync(Ulid tokenId, CancellationToken cancellationToken = default)
    {
        await using TContext context = _contextFactory.CreateDbContext();
        ScheduledMessageRecord? message = await context.Set<ScheduledMessageRecord>()
            .FirstOrDefaultAsync(entity => entity.TokenId == tokenId, cancellationToken)
            .ConfigureAwait(false);

        if (message == null) return false;

        _ = context.Set<ScheduledMessageRecord>().Remove(message);
        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IScheduledMessage?> GetAsync(Ulid tokenId, CancellationToken cancellationToken = default)
    {
        await using TContext context = _contextFactory.CreateDbContext();
        return await context.Set<ScheduledMessageRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TokenId == tokenId, cancellationToken)
            .ConfigureAwait(false);
    }
}
