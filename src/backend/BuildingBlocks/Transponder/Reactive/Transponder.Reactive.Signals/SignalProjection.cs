using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// Folds bus deliveries into a named, process-wide latest-state signal: an in-memory
/// projection in the event-storming sense, never a replayable log (signals die with the
/// process; durability stays with the outbox/inbox — CLAUDE.md's no-event-sourcing rule).
///
/// The consumer itself is registered scoped like every other consumer, but carries no state:
/// it folds into the singleton <see cref="SignalStore"/> via a CAS update, so concurrent
/// deliveries never lose updates. Delivery is at-least-once — <see cref="Reduce"/> must
/// tolerate replay: latest-wins assignment is naturally idempotent, monotonic version guards
/// handle replays and reordering, bare counters are NOT safe without a deduplication key.
/// </summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
/// <typeparam name="TState">The projected state.</typeparam>
public abstract class SignalProjection<TMessage, TState> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly SignalStore _store;

    /// <summary>Creates the projection over the process-wide signal store.</summary>
    protected SignalProjection(SignalStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>Name of the projected state's signal in the <see cref="SignalStore"/>.</summary>
    protected abstract string StateName { get; }

    /// <summary>Produces the initial state on first delivery (or first reader).</summary>
    protected abstract TState CreateInitialState();

    /// <summary>
    /// Pure reducer folding one delivery into the state. May run more than once per delivery
    /// under contention (CAS retry) and must therefore be side-effect free.
    /// </summary>
    protected abstract TState Reduce(TState current, TMessage message);

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _store.GetOrCreate(StateName, CreateInitialState).Update(state => Reduce(state, context.Message));
        return Task.CompletedTask;
    }
}

/// <summary>Registration sugar for signal projections.</summary>
public static class SignalProjectionTransponderBuilderExtensions
{
    extension(TransponderBuilder builder)
    {
        /// <summary>
        /// Registers <typeparamref name="TProjection"/> as a consumer of
        /// <typeparamref name="TMessage"/> and ensures the singleton <see cref="SignalStore"/>
        /// it folds into is available.
        /// </summary>
        public TransponderBuilder AddSignalProjection<TMessage, TProjection>()
            where TMessage : class
            where TProjection : class, IConsumer<TMessage>
        {
            builder.Services.TryAddSingleton<SignalStore>();
            return builder.AddConsumer<TMessage, TProjection>();
        }
    }
}
