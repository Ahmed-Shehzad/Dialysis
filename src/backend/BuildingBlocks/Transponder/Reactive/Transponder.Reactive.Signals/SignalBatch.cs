namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// Batches signal notifications on the current thread: inside <see cref="Run"/>, writes swap
/// values immediately (readers on other threads always see latest state) but notifications are
/// collected, deduplicated per signal, and flushed once when the outermost batch completes —
/// so a computed over two signals written in one batch is invalidated once, and an effect
/// observes only the final state (glitch-free diamonds).
///
/// The batch is thread-bound and synchronous: it deliberately does not flow across
/// <c>await</c> boundaries, and writers on other threads concurrent with a batch are not
/// deferred. Nested batches flush at the outermost scope only.
/// </summary>
public static class SignalBatch
{
    [ThreadStatic]
    private static BatchContext? _current;

    /// <summary>Runs <paramref name="action"/> with notification batching on this thread.</summary>
    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var context = _current;
        if (context is not null)
        {
            // Nested batch: the outermost scope owns the flush.
            action();
            return;
        }

        context = new BatchContext();
        _current = context;
        try
        {
            action();
        }
        finally
        {
            _current = null;
            foreach (var node in context.Pending)
            {
                node.NotifySubscribersNow();
            }
        }
    }

    /// <summary>
    /// Defers <paramref name="node"/>'s notification when a batch is active on this thread.
    /// Returns false (caller notifies immediately) otherwise.
    /// </summary>
    internal static bool TryDefer(ISignalNode node)
    {
        var context = _current;
        if (context is null)
        {
            return false;
        }

        context.Pending.Add(node);
        return true;
    }

    private sealed class BatchContext
    {
        public HashSet<ISignalNode> Pending { get; } = [];
    }
}
