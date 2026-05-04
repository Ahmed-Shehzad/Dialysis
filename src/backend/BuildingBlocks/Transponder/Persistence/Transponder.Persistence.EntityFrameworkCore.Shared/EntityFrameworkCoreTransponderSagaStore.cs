using Dialysis.BuildingBlocks.Transponder;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// <see cref="ITransponderSagaStore"/> backed by EF Core (same database as Transponder outbox/inbox). Safe across nodes when combined with unique
/// (<see cref="TransponderSagaInstanceEntity.SagaKind"/>, <see cref="TransponderSagaInstanceEntity.InstanceKey"/>) and optimistic concurrency on <see cref="TransponderSagaInstanceEntity.Version"/>.
/// </summary>
public sealed class EntityFrameworkCoreTransponderSagaStore<TContext>(TContext db) : ITransponderSagaStore
    where TContext : TransponderPersistenceDbContextBase
{
    public async Task<TransponderSagaRecord?> GetAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default)
    {
        var row = await db.SagaInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.SagaKind == sagaKind && e.InstanceKey == instanceKey,
                cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : ToRecord(row);
    }

    public async Task<bool> TryInsertAsync(TransponderSagaRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        db.SagaInstances.Add(
            new TransponderSagaInstanceEntity
            {
                Id = Guid.NewGuid(),
                SagaKind = record.SagaKind,
                InstanceKey = record.InstanceKey,
                StateName = record.StateName,
                StateJson = record.StateJson,
                Version = record.Version,
                IsCompleted = record.IsCompleted,
                UpdatedAtUtc = DateTime.UtcNow,
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<bool> TryUpdateAsync(TransponderSagaRecord record, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var row = await db.SagaInstances
            .FirstOrDefaultAsync(
                e => e.SagaKind == record.SagaKind && e.InstanceKey == record.InstanceKey,
                cancellationToken)
            .ConfigureAwait(false);

        if (row is null || row.Version != expectedVersion || row.IsCompleted)
            return false;

        row.StateName = record.StateName;
        row.StateJson = record.StateJson;
        row.Version = record.Version;
        row.IsCompleted = record.IsCompleted;
        row.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task DeleteAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default)
    {
        await db.SagaInstances
            .Where(e => e.SagaKind == sagaKind && e.InstanceKey == instanceKey)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static TransponderSagaRecord ToRecord(TransponderSagaInstanceEntity e) => new()
    {
        SagaKind = e.SagaKind,
        InstanceKey = e.InstanceKey,
        StateName = e.StateName,
        StateJson = e.StateJson,
        Version = e.Version,
        IsCompleted = e.IsCompleted,
    };
}
