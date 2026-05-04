using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Process-local saga storage for development and tests. For production across multiple nodes, register
/// <c>EntityFrameworkCoreTransponderSagaStore&lt;TContext&gt;</c> with <c>AddTransponderEfSagaStore&lt;TContext&gt;()</c> from the Transponder EF persistence package (same database as outbox/inbox).
/// </summary>
public sealed class InMemoryTransponderSagaStore : ITransponderSagaStore
{
    private readonly ConcurrentDictionary<string, TransponderSagaRecord> _rows = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    private static string RowKey(string sagaKind, string instanceKey) => sagaKind + "\u001f" + instanceKey;

    public Task<TransponderSagaRecord?> GetAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TransponderSagaRecord? result;
        lock (_sync)
            result = _rows.TryGetValue(RowKey(sagaKind, instanceKey), out var r) ? Clone(r) : null;

        return Task.FromResult(result);
    }

    public Task<bool> TryInsertAsync(TransponderSagaRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool ok;
        lock (_sync)
        {
            var key = RowKey(record.SagaKind, record.InstanceKey);
            ok = _rows.TryAdd(key, Clone(record));
        }

        return Task.FromResult(ok);
    }

    public Task<bool> TryUpdateAsync(TransponderSagaRecord record, long expectedVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool ok;
        lock (_sync)
        {
            var key = RowKey(record.SagaKind, record.InstanceKey);
            if (!_rows.TryGetValue(key, out var current) || current.Version != expectedVersion || current.IsCompleted)
                ok = false;
            else
            {
                _rows[key] = Clone(record);
                ok = true;
            }
        }

        return Task.FromResult(ok);
    }

    public Task DeleteAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _rows.TryRemove(RowKey(sagaKind, instanceKey), out _);
        }

        return Task.CompletedTask;
    }

    private static TransponderSagaRecord Clone(TransponderSagaRecord r) => new()
    {
        SagaKind = r.SagaKind,
        InstanceKey = r.InstanceKey,
        StateName = r.StateName,
        StateJson = r.StateJson,
        Version = r.Version,
        IsCompleted = r.IsCompleted,
    };
}
