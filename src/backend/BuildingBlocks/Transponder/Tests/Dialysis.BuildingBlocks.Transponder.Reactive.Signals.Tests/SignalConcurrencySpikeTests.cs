using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

/// <summary>
/// The concurrency spike suite from the signals audit (§5.3) — the gate that must stay green
/// before signals may move beyond the diagnostics/projection state plane.
/// </summary>
public class SignalConcurrencySpikeTests
{
    [Fact]
    public async Task Parallel_Writers_On_Distinct_Signals_Yield_Consistent_Computed_Sum_Async()
    {
        const int signalCount = 8;
        const int incrementsPerSignal = 5_000;
        var signals = Enumerable.Range(0, signalCount).Select(_ => new Signal<long>(0)).ToArray();
        var sum = Computed.FromMany<long, long>(signals, values => values.Sum());

        var writers = signals.Select(signal => Task.Run(() =>
        {
            for (var i = 0; i < incrementsPerSignal; i++)
            {
                signal.Update(v => v + 1);
            }
        }));
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            // Concurrent reads must always be a valid partial sum, never garbage.
            for (var i = 0; i < 2_000; i++)
            {
                var observed = sum.Value;
                Assert.InRange(observed, 0, (long)signalCount * incrementsPerSignal);
            }
        }));

        await Task.WhenAll(writers.Concat(readers));

        Assert.Equal((long)signalCount * incrementsPerSignal, sum.Value);
    }

    [Fact]
    public async Task Parallel_Writers_On_One_Signal_Apply_Every_Update_Exactly_Once_Async()
    {
        const int writerCount = 8;
        const int iterations = 10_000;
        var signal = new Signal<long>(0);

        await Task.WhenAll(Enumerable.Range(0, writerCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                signal.Update(v => v + 1);
            }
        })));

        // Update is a CAS retry loop: read-modify-write never loses an increment.
        Assert.Equal((long)writerCount * iterations, signal.Value);
        Assert.Equal((long)writerCount * iterations, signal.Version);
    }

    [Fact]
    public async Task Concurrent_Reads_Never_Observe_Torn_Snapshots_Async()
    {
        var signal = new Signal<(long Positive, long Negative)>((0, 0));
        using var stop = new CancellationTokenSource();

        var writer = Task.Run(() =>
        {
            var n = 0L;
            while (!stop.Token.IsCancellationRequested)
            {
                n++;
                var value = (n, -n);
                signal.Value = value;
            }
        });

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 200_000; i++)
            {
                var (positive, negative) = signal.Value;
                // The invariant travels in one immutable snapshot: tearing is impossible.
                Assert.Equal(positive, -negative);
            }
        })).ToArray();

        await Task.WhenAll(readers);
        await stop.CancelAsync();
        await writer;
    }

    [Fact]
    public async Task Computed_Read_Race_Returns_Value_From_Coherent_Dependency_Versions_Async()
    {
        var left = new Signal<long>(0);
        var right = new Signal<long>(0);
        var pair = Computed.From(left, right, (a, b) => (A: a, B: b));
        using var stop = new CancellationTokenSource();

        var writer = Task.Run(() =>
        {
            var n = 0L;
            while (!stop.Token.IsCancellationRequested)
            {
                n++;
                // Writes are individually atomic; the computed's seqlock validation must
                // never expose a pair captured across two different write generations
                // where left ran ahead of right by more than the in-flight delta.
                left.Value = n;
                right.Value = n;
            }
        });

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 100_000; i++)
            {
                var (a, b) = pair.Value;
                // b can only be the same generation as a or one behind it — never ahead,
                // and never a stale mix from a torn capture.
                Assert.InRange(a - b, 0, 1);
            }
        })).ToArray();

        await Task.WhenAll(readers);
        await stop.CancelAsync();
        await writer;
    }
}
