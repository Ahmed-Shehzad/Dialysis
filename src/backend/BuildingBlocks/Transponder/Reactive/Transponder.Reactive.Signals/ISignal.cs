namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// Non-generic surface every signal node exposes: a monotonically increasing version and
/// change subscription. Subscriptions are strong references; dispose the returned token to
/// detach (see the project README for lifetime rules).
/// </summary>
public interface ISignal
{
    /// <summary>Monotonically increasing version; bumps whenever the observable value changes.</summary>
    long Version { get; }

    /// <summary>
    /// Registers a change callback. The callback MUST be cheap and non-blocking (it runs on the
    /// writer's thread during notification fan-out) — typical subscribers are computed-signal
    /// invalidation marks and effect wake-ups, never user work.
    /// </summary>
    IDisposable Subscribe(Action onChanged);
}

/// <summary>A signal whose current value can be read. Reads are always torn-free snapshots.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReadOnlySignal<out T> : ISignal
{
    /// <summary>The current value.</summary>
    T Value { get; }
}

/// <summary>
/// Internal contract for nodes that can be asked to fan out a pending notification — used by
/// <see cref="SignalBatch"/> to defer and deduplicate notifications until the outermost batch
/// completes.
/// </summary>
internal interface ISignalNode
{
    /// <summary>Notifies subscribers immediately, bypassing any batch deferral.</summary>
    void NotifySubscribersNow();
}

/// <summary>
/// Internal contract for reading a node's value and version as one coherent (non-torn) pair —
/// the primitive the computed seqlock builds on. Implemented by <see cref="Signal{T}"/> and
/// computed signals; computed dependencies must be nodes from this library.
/// </summary>
internal interface ICoherentRead
{
    /// <summary>Reads the current value (boxed) and its version atomically.</summary>
    object? ReadBoxed(out long version);
}
