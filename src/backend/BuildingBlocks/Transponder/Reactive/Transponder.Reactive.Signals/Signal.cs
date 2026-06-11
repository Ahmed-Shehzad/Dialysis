namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// A writable, thread-safe signal. The value and its version travel together in one immutable
/// snapshot object swapped atomically, so concurrent readers can never observe a torn value —
/// even for large structs or multi-field records.
///
/// Write semantics: setting <see cref="Value"/> is last-write-wins under concurrency;
/// <see cref="Update"/> is a compare-and-swap retry loop and is the safe choice for
/// read-modify-write (counters, dictionary folds, reducers). Subscriber callbacks run on the
/// writer's thread after the swap completes and never under internal locks.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Signal<T> : IReadOnlySignal<T>, ISignalNode, ICoherentRead
{
    private sealed class Snapshot
    {
        public Snapshot(T value, long version)
        {
            Value = value;
            Version = version;
        }

        public T Value { get; }

        public long Version { get; }
    }

    private readonly IEqualityComparer<T> _comparer;
    private readonly SubscriberList _subscribers = new();
    private Snapshot _current;

    /// <summary>Creates a signal with an initial value.</summary>
    /// <param name="initialValue">The starting value (version 0).</param>
    /// <param name="comparer">
    /// Optional comparer used to suppress notifications when a write produces an equal value.
    /// Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    public Signal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _current = new Snapshot(initialValue, 0);
    }

    /// <summary>
    /// The current value. The setter swaps atomically and is last-write-wins under concurrency;
    /// use <see cref="Update"/> when the new value depends on the old one.
    /// </summary>
    public T Value
    {
        get => Volatile.Read(ref _current).Value;
        set => Update(_ => value);
    }

    /// <inheritdoc />
    public long Version => Volatile.Read(ref _current).Version;

    /// <summary>Number of attached subscribers (test/diagnostic surface).</summary>
    internal int SubscriberCount => _subscribers.Count;

    /// <summary>
    /// Atomically applies <paramref name="transform"/> to the current value via a
    /// compare-and-swap retry loop — concurrent updates are never lost. The transform may run
    /// more than once under contention and must therefore be pure. Returns the value that won.
    /// </summary>
    public T Update(Func<T, T> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        while (true)
        {
            var observed = Volatile.Read(ref _current);
            var next = transform(observed.Value);
            if (_comparer.Equals(observed.Value, next))
            {
                return observed.Value;
            }

            var candidate = new Snapshot(next, observed.Version + 1);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _current, candidate, observed), observed))
            {
                NotifyChanged();
                return next;
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);
        return _subscribers.Add(onChanged);
    }

    /// <summary>Reads the value and its version as one coherent pair.</summary>
    internal T Read(out long version)
    {
        var snapshot = Volatile.Read(ref _current);
        version = snapshot.Version;
        return snapshot.Value;
    }

    object? ICoherentRead.ReadBoxed(out long version) => Read(out version);

    void ISignalNode.NotifySubscribersNow() => _subscribers.Invoke();

    private void NotifyChanged()
    {
        if (!SignalBatch.TryDefer(this))
        {
            _subscribers.Invoke();
        }
    }
}

/// <summary>
/// Copy-on-write subscriber list shared by signal nodes: subscribe/dispose swap an immutable
/// array under a private lock (rare operations), while notification iterates a lock-free
/// snapshot — user-supplied callbacks never run while a lock is held.
/// </summary>
internal sealed class SubscriberList
{
    private readonly object _mutationLock = new();
    private Subscription[] _items = [];

    public int Count => Volatile.Read(ref _items).Length;

    public IDisposable Add(Action onChanged)
    {
        var subscription = new Subscription(this, onChanged);
        lock (_mutationLock)
        {
            var current = _items;
            var next = new Subscription[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = subscription;
            Volatile.Write(ref _items, next);
        }

        return subscription;
    }

    public void Invoke()
    {
        var snapshot = Volatile.Read(ref _items);
        foreach (var subscription in snapshot)
        {
            subscription.Invoke();
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_mutationLock)
        {
            var current = _items;
            var index = Array.IndexOf(current, subscription);
            if (index < 0)
            {
                return;
            }

            var next = new Subscription[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            Volatile.Write(ref _items, next);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly SubscriberList _owner;
        private Action? _onChanged;

        public Subscription(SubscriberList owner, Action onChanged)
        {
            _owner = owner;
            _onChanged = onChanged;
        }

        public void Invoke() => Volatile.Read(ref _onChanged)?.Invoke();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _onChanged, null) is not null)
            {
                _owner.Remove(this);
            }
        }
    }
}
