using System.Collections.Concurrent;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// In-memory saga repository for development and testing.
/// </summary>
public sealed class InMemorySagaRepository<TState> : ISagaRepository<TState>
    where TState : class, ISagaState
{
    private readonly ConcurrentDictionary<Ulid, TState> _states = new();

    /// <inheritdoc />
    public Task<TState?> GetAsync(Ulid correlationId, CancellationToken cancellationToken = default)
    {
        _ = _states.TryGetValue(correlationId, out TState? state);
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task<bool> SaveAsync(TState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.CorrelationId == Ulid.Empty) throw new ArgumentException("CorrelationId must be provided.", nameof(state));

        if (_states.TryGetValue(state.CorrelationId, out TState? existing))
        {
            // Optimistic concurrency check
            if (existing.Version != state.Version) return Task.FromResult(false);

            state.Version++;
        }
        else state.Version = 1;

        _states[state.CorrelationId] = state;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Ulid correlationId, CancellationToken cancellationToken = default)
    {
        _ = _states.TryRemove(correlationId, out _);
        return Task.CompletedTask;
    }
}
