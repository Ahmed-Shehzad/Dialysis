using System.Collections.Concurrent;

namespace Dialysis.SmartConnect;

/// <summary>
/// In-memory implementation of <see cref="IVariableMapStore"/> for all scopes.
/// GlobalChannel and Global scopes persist in ConcurrentDictionary for the server lifetime.
/// Channel scope is transient (callers create a new dictionary per dispatch).
/// Configuration scope is writable via admin API only.
/// </summary>
public sealed class InMemoryVariableMapStore : IVariableMapStore
{
    // Key = (Scope, FlowId ?? Guid.Empty, Key)
    private readonly ConcurrentDictionary<(VariableMapScope Scope, Guid FlowId, string Key), string> _store = new();

    public Task<string?> GetAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default)
    {
        var found = _store.TryGetValue((scope, flowId ?? Guid.Empty, key), out var value);
        return Task.FromResult(found ? value : null);
    }

    public Task SetAsync(VariableMapScope scope, Guid? flowId, string key, string value, CancellationToken cancellationToken = default)
    {
        _store[(scope, flowId ?? Guid.Empty, key)] = value;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(VariableMapScope scope, Guid? flowId, CancellationToken cancellationToken = default)
    {
        var resolvedFlowId = flowId ?? Guid.Empty;
        var result = new Dictionary<string, string>();
        foreach (var kvp in _store)
        {
            if (kvp.Key.Scope == scope && kvp.Key.FlowId == resolvedFlowId)
            {
                result[kvp.Key.Key] = kvp.Value;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
    }

    public Task RemoveAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove((scope, flowId ?? Guid.Empty, key), out _);
        return Task.CompletedTask;
    }
}
