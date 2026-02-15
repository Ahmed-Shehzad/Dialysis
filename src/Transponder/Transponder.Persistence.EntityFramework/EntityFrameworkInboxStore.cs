using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework inbox store implementation.
/// </summary>
public sealed class EntityFrameworkInboxStore : IInboxStore
{
    private readonly DbContext _context;

    public EntityFrameworkInboxStore(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IInboxState?> GetAsync(
        Ulid messageId,
        string consumerId,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(consumerId)
            ? throw new ArgumentException("ConsumerId must be provided.", nameof(consumerId))
            : (IInboxState?)await _context.Set<InboxStateEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                state => state.MessageId == messageId && state.ConsumerId == consumerId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(IInboxState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Check if entity already exists (either tracked or in database)
        var existing = await _context.Set<InboxStateEntity>()
            .FindAsync([state.MessageId, state.ConsumerId], cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
            // Entity already exists
            return false;

        var entity = InboxStateEntity.FromState(state);

        try
        {
            _ = await _context.Set<InboxStateEntity>()
                .AddAsync(entity, cancellationToken)
                .ConfigureAwait(false);

            _ = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE") == true ||
                                            ex.InnerException?.Message?.Contains("duplicate") == true ||
                                            ex.InnerException?.Message?.Contains("unique constraint") == true)
        {
            // Race condition: another request already added this inbox state
            // This is expected in concurrent scenarios, return false to indicate it already exists
            return false;
        }
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(
        Ulid messageId,
        string consumerId,
        DateTimeOffset processedTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(consumerId)) throw new ArgumentException("ConsumerId must be provided.", nameof(consumerId));

        var state = await _context.Set<InboxStateEntity>()
            .FirstOrDefaultAsync(
                entry => entry.MessageId == messageId && entry.ConsumerId == consumerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (state == null) return;

        state.ProcessedTime = processedTime;
    }
}
