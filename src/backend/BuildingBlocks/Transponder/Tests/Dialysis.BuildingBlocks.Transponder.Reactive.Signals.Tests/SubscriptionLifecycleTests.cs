using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class SubscriptionLifecycleTests
{
    [Fact]
    public void Subscribe_Dispose_Cycles_Leave_No_Subscribers()
    {
        var source = new Signal<int>(0);
        for (var i = 0; i < 100_000; i++)
        {
            var subscription = source.Subscribe(static () => { });
            subscription.Dispose();
        }

        Assert.Equal(0, source.SubscriberCount);
    }

    [Fact]
    public void Disposed_Computed_Detaches_From_Dependencies()
    {
        var source = new Signal<int>(0);
        var computed = (ComputedSignal<int>)Computed.From(source, v => v + 1);
        Assert.Equal(1, source.SubscriberCount);

        computed.Dispose();

        Assert.Equal(0, source.SubscriberCount);
    }

    [Fact]
    public void Double_Dispose_Of_A_Subscription_Is_Safe()
    {
        var source = new Signal<int>(0);
        var first = source.Subscribe(static () => { });
        var second = source.Subscribe(static () => { });

        first.Dispose();
        first.Dispose();

        Assert.Equal(1, source.SubscriberCount);
        second.Dispose();
        Assert.Equal(0, source.SubscriberCount);
    }

    [Fact]
    public async Task Collected_Effect_Is_Not_Pinned_By_Source_After_Dispose_Async()
    {
        var source = new Signal<int>(0);
        var weakEffect = await Create_And_Dispose_Effect_Async(source);

        // Forced collection is the point of this leak test: a disposed effect must be
        // collectible, i.e. the source's subscriber list no longer pins it.
#pragma warning disable S1215
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
#pragma warning restore S1215

        Assert.False(weakEffect.TryGetTarget(out _));
        Assert.Equal(0, source.SubscriberCount);
    }

    private static async Task<WeakReference<Effect>> Create_And_Dispose_Effect_Async(Signal<int> source)
    {
        var effect = new Effect([source], static () => { });
        await effect.DisposeAsync();
        return new WeakReference<Effect>(effect);
    }
}
