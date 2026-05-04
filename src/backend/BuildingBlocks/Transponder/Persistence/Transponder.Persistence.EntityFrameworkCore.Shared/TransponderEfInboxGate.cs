using Dialysis.BuildingBlocks.Transponder;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// EF-backed inbox using a unique <see cref="TransponderInboxMessageEntity.DeduplicationKey"/>.
/// </summary>
public sealed class TransponderEfInboxGate<TContext>(TContext db) : ITransponderInboxGate
    where TContext : TransponderPersistenceDbContextBase
{
    public async Task<bool> TryAcquireAsync(string deduplicationKey, string routingKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        var entity = new TransponderInboxMessageEntity
        {
            Id = Guid.NewGuid(),
            DeduplicationKey = deduplicationKey,
            RoutingKey = routingKey,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.InboxMessages.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            var row = await db.InboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeduplicationKey == deduplicationKey, cancellationToken)
                .ConfigureAwait(false);
            if (row is null)
                throw;

            if (row.CompletedAtUtc is not null)
                return false;
            return true;
        }
    }

    public async Task CompleteAsync(string deduplicationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);
        await db.InboxMessages
            .Where(x => x.DeduplicationKey == deduplicationKey && x.CompletedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.CompletedAtUtc, DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AbandonAsync(string deduplicationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);
        await db.InboxMessages
            .Where(x => x.DeduplicationKey == deduplicationKey && x.CompletedAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
