namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// Factories for computed signals with <b>explicit</b> dependencies — no auto-tracking magic,
/// by design: explicit dependency lists keep the graph statically known and safe under
/// server-side concurrency (UI-style auto-tracking relies on thread-affine ambient state; see
/// docs/reports/signals-transponder-audit-2026-06-11.md). Dependencies must be nodes from this
/// library (<see cref="Signal{T}"/> or other computed signals).
///
/// Computeds are push-invalidated (a dependency change marks them dirty and propagates down
/// chains without recomputing) and pull-recomputed with seqlock validation: the compute runs
/// over a captured set of dependency snapshots and retries if any dependency moved
/// mid-compute, so readers always see a value derived from one coherent dependency state.
/// The compute delegate must be pure over its inputs; it runs on the reader's thread, never
/// under locks, and may run more than once under contention.
/// </summary>
public static class Computed
{
    /// <summary>Creates a computed signal over one dependency.</summary>
    public static IReadOnlySignal<TResult> From<T1, TResult>(
        IReadOnlySignal<T1> source,
        Func<T1, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return new ComputedSignal<TResult>(
            [RequireNode(source)],
            values => compute((T1)values[0]!),
            comparer);
    }

    /// <summary>Creates a computed signal over two dependencies.</summary>
    public static IReadOnlySignal<TResult> From<T1, T2, TResult>(
        IReadOnlySignal<T1> source1,
        IReadOnlySignal<T2> source2,
        Func<T1, T2, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return new ComputedSignal<TResult>(
            [RequireNode(source1), RequireNode(source2)],
            values => compute((T1)values[0]!, (T2)values[1]!),
            comparer);
    }

    /// <summary>Creates a computed signal over three dependencies.</summary>
    public static IReadOnlySignal<TResult> From<T1, T2, T3, TResult>(
        IReadOnlySignal<T1> source1,
        IReadOnlySignal<T2> source2,
        IReadOnlySignal<T3> source3,
        Func<T1, T2, T3, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return new ComputedSignal<TResult>(
            [RequireNode(source1), RequireNode(source2), RequireNode(source3)],
            values => compute((T1)values[0]!, (T2)values[1]!, (T3)values[2]!),
            comparer);
    }

    /// <summary>Creates a computed signal over four dependencies.</summary>
    public static IReadOnlySignal<TResult> From<T1, T2, T3, T4, TResult>(
        IReadOnlySignal<T1> source1,
        IReadOnlySignal<T2> source2,
        IReadOnlySignal<T3> source3,
        IReadOnlySignal<T4> source4,
        Func<T1, T2, T3, T4, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return new ComputedSignal<TResult>(
            [RequireNode(source1), RequireNode(source2), RequireNode(source3), RequireNode(source4)],
            values => compute((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!),
            comparer);
    }

    /// <summary>
    /// Creates a computed signal over a homogeneous list of dependencies; the compute receives
    /// a coherent value array in dependency order.
    /// </summary>
    public static IReadOnlySignal<TResult> FromMany<TSource, TResult>(
        IReadOnlyList<IReadOnlySignal<TSource>> sources,
        Func<IReadOnlyList<TSource>, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(compute);
        var dependencies = new DependencyNode[sources.Count];
        for (var i = 0; i < sources.Count; i++)
        {
            dependencies[i] = RequireNode(sources[i]);
        }

        return new ComputedSignal<TResult>(
            dependencies,
            values =>
            {
                var typed = new TSource[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    typed[i] = (TSource)values[i]!;
                }

                return compute(typed);
            },
            comparer);
    }

    private static DependencyNode RequireNode(ISignal source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is not ICoherentRead coherent)
        {
            throw new ArgumentException(
                $"Computed dependencies must be Signal<T> or computed signals from this library; got {source.GetType()}.",
                nameof(source));
        }

        return new DependencyNode(source, coherent);
    }
}

/// <summary>A computed dependency: the public node for subscription plus its coherent-read surface.</summary>
internal readonly record struct DependencyNode(ISignal Signal, ICoherentRead Reader);

/// <summary>
/// A derived signal over explicit dependencies. Dirty-marked on dependency change
/// (push-invalidate), recomputed lazily on read with seqlock validation against the captured
/// dependency versions (pull-recompute), memoized while dependency versions are unchanged.
/// </summary>
/// <typeparam name="T">The computed value type.</typeparam>
internal sealed class ComputedSignal<T> : IReadOnlySignal<T>, ISignalNode, ICoherentRead, IDisposable
{
    private const int MaxRecomputeSpins = 16;

    private sealed class Cache
    {
        public Cache(T value, long version, long[] dependencyVersions)
        {
            Value = value;
            Version = version;
            DependencyVersions = dependencyVersions;
        }

        public T Value { get; }

        public long Version { get; }

        public long[] DependencyVersions { get; }
    }

    private readonly DependencyNode[] _dependencies;
    private readonly Func<object?[], T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private readonly SubscriberList _subscribers = new();
    private readonly IDisposable[] _dependencySubscriptions;
    private Cache? _cache;
    private int _dirty = 1;
    private int _disposed;

    public ComputedSignal(DependencyNode[] dependencies, Func<object?[], T> compute, IEqualityComparer<T>? comparer)
    {
        _dependencies = dependencies;
        _compute = compute;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _dependencySubscriptions = new IDisposable[dependencies.Length];
        for (var i = 0; i < dependencies.Length; i++)
        {
            _dependencySubscriptions[i] = dependencies[i].Signal.Subscribe(MarkDirty);
        }
    }

    public T Value => EnsureFresh().Value;

    public long Version => EnsureFresh().Version;

    /// <summary>Number of attached subscribers (test/diagnostic surface).</summary>
    internal int SubscriberCount => _subscribers.Count;

    /// <summary>How many times the compute delegate has executed (test/diagnostic surface).</summary>
    internal int ComputeCount => Volatile.Read(ref _computeCount);

    private int _computeCount;

    public IDisposable Subscribe(Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);
        return _subscribers.Add(onChanged);
    }

    /// <summary>Detaches this computed from all of its dependencies.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (var subscription in _dependencySubscriptions)
        {
            subscription.Dispose();
        }
    }

    object? ICoherentRead.ReadBoxed(out long version)
    {
        var cache = EnsureFresh();
        version = cache.Version;
        return cache.Value;
    }

    void ISignalNode.NotifySubscribersNow() => _subscribers.Invoke();

    private void MarkDirty()
    {
        if (Interlocked.Exchange(ref _dirty, 1) == 0)
        {
            // First transition to dirty since the last recompute: propagate the invalidation
            // down the graph (subscribers are downstream computeds and effect wakes — O(1)).
            if (!SignalBatch.TryDefer(this))
            {
                _subscribers.Invoke();
            }
        }
    }

    private Cache EnsureFresh()
    {
        var cache = Volatile.Read(ref _cache);
        if (cache is not null && Volatile.Read(ref _dirty) == 0)
        {
            return cache;
        }

        for (var spin = 0; spin < MaxRecomputeSpins; spin++)
        {
            // Clear dirty BEFORE validating, so a ping landing mid-compute re-dirties us and
            // forces another pass instead of being lost.
            Interlocked.Exchange(ref _dirty, 0);

            var capturedVersions = new long[_dependencies.Length];
            var capturedValues = new object?[_dependencies.Length];
            for (var i = 0; i < _dependencies.Length; i++)
            {
                capturedValues[i] = _dependencies[i].Reader.ReadBoxed(out capturedVersions[i]);
            }

            var value = _compute(capturedValues);
            Interlocked.Increment(ref _computeCount);

            var moved = false;
            for (var i = 0; i < _dependencies.Length; i++)
            {
                _ = _dependencies[i].Reader.ReadBoxed(out var versionNow);
                if (versionNow != capturedVersions[i])
                {
                    moved = true;
                    break;
                }
            }

            if (moved)
            {
                Interlocked.Exchange(ref _dirty, 1);
                continue;
            }

            var previous = Volatile.Read(ref _cache);
            Cache next;
            if (previous is not null && _comparer.Equals(previous.Value, value))
            {
                // Value unchanged: keep the version (downstream memoization stays valid),
                // refresh the dependency-version vector.
                next = new Cache(previous.Value, previous.Version, capturedVersions);
            }
            else
            {
                next = new Cache(value, (previous?.Version ?? -1) + 1, capturedVersions);
            }

            Volatile.Write(ref _cache, next);
            if (Volatile.Read(ref _dirty) == 1)
            {
                // A dependency moved between validation and publish; retry on the next loop
                // iteration (or leave dirty for the next reader if spins are exhausted).
                continue;
            }

            return next;
        }

        // Contention exhausted the spin budget: return the last coherent value; dirty stays
        // set so the next read retries.
        return Volatile.Read(ref _cache) ?? throw new InvalidOperationException(
            "Computed signal could not produce an initial value under sustained dependency contention.");
    }
}
