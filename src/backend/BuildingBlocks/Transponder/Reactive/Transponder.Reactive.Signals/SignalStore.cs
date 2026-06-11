using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// Singleton registry of named signals — the seam between scoped message consumers and
/// process-wide reactive state. Consumers (new instance per delivery scope) resolve the store
/// and fold into the same named signal; readers (dashboards, SignalR pushers, health
/// endpoints) resolve the store and observe it. Names are unique per state type.
/// </summary>
public sealed class SignalStore
{
    private readonly ConcurrentDictionary<(string Name, Type StateType), Lazy<object>> _signals = new();

    /// <summary>
    /// Gets the named signal, creating it with <paramref name="initialFactory"/> on first use
    /// (the factory runs at most once).
    /// </summary>
    public Signal<TState> GetOrCreate<TState>(string name, Func<TState> initialFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(initialFactory);
        var lazy = _signals.GetOrAdd(
            (name, typeof(TState)),
            static (_, factory) => new Lazy<object>(() => new Signal<TState>(factory()), LazyThreadSafetyMode.ExecutionAndPublication),
            initialFactory);
        return (Signal<TState>)lazy.Value;
    }

    /// <summary>Returns the named signal as a read-only view, or null when it does not exist yet.</summary>
    public IReadOnlySignal<TState>? TryGet<TState>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _signals.TryGetValue((name, typeof(TState)), out var lazy)
            ? (Signal<TState>)lazy.Value
            : null;
    }
}
