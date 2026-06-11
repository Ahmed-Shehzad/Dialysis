using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class SignalBatchTests
{
    [Fact]
    public void Batch_Defers_Notifications_Until_Completion()
    {
        var source = new Signal<int>(0);
        var notifications = 0;
        using var subscription = source.Subscribe(() => notifications++);

        SignalBatch.Run(() =>
        {
            source.Value = 1;
            Assert.Equal(0, notifications);
        });

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void Batch_Dedupes_Notifications_Per_Signal()
    {
        var source = new Signal<int>(0);
        var notifications = 0;
        using var subscription = source.Subscribe(() => notifications++);

        SignalBatch.Run(() =>
        {
            source.Value = 1;
            source.Value = 2;
            source.Value = 3;
        });

        Assert.Equal(1, notifications);
        Assert.Equal(3, source.Value);
    }

    [Fact]
    public async Task Effect_Inside_Batch_Observes_Only_Final_State_Async()
    {
        var left = new Signal<int>(0);
        var right = new Signal<int>(0);
        var pair = Computed.From(left, right, (a, b) => (A: a, B: b));

        var observed = new List<(int A, int B)>();
        var observedFinal = new TaskCompletionSource();
        await using var effect = new Effect([pair], () =>
        {
            var value = pair.Value;
            lock (observed)
            {
                observed.Add(value);
            }

            if (value is { A: 1, B: 1 })
            {
                observedFinal.TrySetResult();
            }
        });

        SignalBatch.Run(() =>
        {
            left.Value = 1;
            right.Value = 1;
        });

        await observedFinal.Task.WaitAsync(TimeSpan.FromSeconds(10));

        lock (observed)
        {
            // The startup run sees (0,0); the batched write must surface only as (1,1) —
            // the intermediate (1,0) diamond glitch never materializes.
            Assert.DoesNotContain((1, 0), observed);
            Assert.DoesNotContain((0, 1), observed);
        }
    }

    [Fact]
    public void Nested_Batches_Flush_Once_At_Outermost_Scope()
    {
        var source = new Signal<int>(0);
        var notifications = 0;
        using var subscription = source.Subscribe(() => notifications++);

        SignalBatch.Run(() =>
        {
            source.Value = 1;
            SignalBatch.Run(() => source.Value = 2);
            Assert.Equal(0, notifications);
        });

        Assert.Equal(1, notifications);
    }
}
