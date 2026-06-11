using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class EffectTests
{
    [Fact]
    public async Task Effect_Runs_When_Source_Changes_Async()
    {
        var source = new Signal<int>(0);
        var observedTen = new TaskCompletionSource();
        await using var effect = new Effect([source], () =>
        {
            if (source.Value == 10)
            {
                observedTen.TrySetResult();
            }
        });

        source.Value = 10;

        await observedTen.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Effect_Coalesces_Rapid_Changes_To_Latest_State_Async()
    {
        const int writes = 1_000;
        var source = new Signal<int>(0);
        using var gate = new SemaphoreSlim(0);
        var runs = 0;
        var lastObserved = -1;
        var observedFinal = new TaskCompletionSource();

        await using var effect = new Effect([source], async token =>
        {
            // Block the first run until all writes happened, so the burst must coalesce.
            await gate.WaitAsync(token).ConfigureAwait(false);
            Interlocked.Increment(ref runs);
            var value = source.Value;
            Volatile.Write(ref lastObserved, value);
            if (value == writes)
            {
                observedFinal.TrySetResult();
            }
        });

        for (var i = 1; i <= writes; i++)
        {
            source.Value = i;
        }

        gate.Release(writes);
        await observedFinal.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Bounded(1) drop-write wake: the whole burst collapses into at most the in-flight
        // run plus one queued follow-up (plus the startup prime).
        Assert.InRange(Volatile.Read(ref runs), 1, 3);
        Assert.Equal(writes, Volatile.Read(ref lastObserved));
    }

    [Fact]
    public async Task Effect_Failure_Is_Logged_And_Does_Not_Poison_The_Graph_Async()
    {
        var source = new Signal<int>(0);
        var observedTwo = new TaskCompletionSource();
        await using var effect = new Effect([source], () =>
        {
            if (source.Value == 1)
            {
                throw new InvalidOperationException("Deliberate test failure.");
            }

            if (source.Value == 2)
            {
                observedTwo.TrySetResult();
            }
        });

        source.Value = 1;
        // Give the failing run a moment to execute, then prove the effect is still alive.
        await Task.Delay(50);
        source.Value = 2;

        await observedTwo.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, source.Value);
    }

    [Fact]
    public async Task Effect_Runs_Without_A_Synchronization_Context_Async()
    {
        var source = new Signal<int>(0);
        var captured = new TaskCompletionSource<SynchronizationContext?>();
        await using var effect = new Effect([source], () =>
        {
            if (source.Value == 1)
            {
                captured.TrySetResult(SynchronizationContext.Current);
            }
        });

        source.Value = 1;

        var contextInsideReaction = await captured.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Null(contextInsideReaction);
    }

    [Fact]
    public async Task Effect_Stops_After_Dispose_Async()
    {
        var source = new Signal<int>(0);
        var runs = 0;
        var effect = new Effect([source], () => Interlocked.Increment(ref runs));
        await effect.DisposeAsync();

        var runsAfterDispose = Volatile.Read(ref runs);
        source.Value = 1;
        await Task.Delay(100);

        Assert.Equal(runsAfterDispose, Volatile.Read(ref runs));
        Assert.Equal(0, GetSubscriberCount(source));
    }

    private static int GetSubscriberCount(Signal<int> signal) => signal.SubscriberCount;
}
