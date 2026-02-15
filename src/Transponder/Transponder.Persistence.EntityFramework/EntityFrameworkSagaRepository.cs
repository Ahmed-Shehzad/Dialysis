using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework saga repository implementation.
/// </summary>
public sealed class EntityFrameworkSagaRepository<TState> : ISagaRepository<TState>
    where TState : class, ISagaState
{
    private readonly DbContext _context;
    private readonly string _stateType;

    public EntityFrameworkSagaRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _stateType = typeof(TState).FullName ?? typeof(TState).Name;
    }

    /// <inheritdoc />
    public async Task<TState?> GetAsync(Ulid correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<SagaStateEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                state => state.CorrelationId == correlationId && state.StateType == _stateType,
                cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToState<TState>();
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(TState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.CorrelationId == Ulid.Empty) throw new ArgumentException("CorrelationId must be provided.", nameof(state));

        var existing = await _context.Set<SagaStateEntity>()
            .FirstOrDefaultAsync(
                entity => entity.CorrelationId == state.CorrelationId && entity.StateType == _stateType,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            state.Version = 1;
            var entity = SagaStateEntity.FromState(state);
            _ = await _context.Set<SagaStateEntity>()
                .AddAsync(entity, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        // Optimistic concurrency check
        if (existing.Version != state.Version) return false;

        state.Version++;
        var updated = SagaStateEntity.FromState(state);
        existing.ConversationId = updated.ConversationId;
        existing.StateData = updated.StateData;
        existing.UpdatedTime = updated.UpdatedTime;
        existing.Version = updated.Version;

        return true;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Ulid correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<SagaStateEntity>()
            .FirstOrDefaultAsync(
                state => state.CorrelationId == correlationId && state.StateType == _stateType,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null) return;

        _ = _context.Set<SagaStateEntity>().Remove(entity);
    }
}
