using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class ComputedSignalTests
{
    [Fact]
    public void Computed_Recomputes_When_Dependency_Changes()
    {
        var source = new Signal<int>(1);
        var doubled = Computed.From(source, v => v * 2);

        Assert.Equal(2, doubled.Value);
        source.Value = 21;
        Assert.Equal(42, doubled.Value);
    }

    [Fact]
    public void Computed_Memoizes_When_Dependency_Versions_Unchanged()
    {
        var source = new Signal<int>(1);
        var computed = (ComputedSignal<int>)Computed.From(source, v => v * 2);

        _ = computed.Value;
        var computeCountAfterFirstRead = computed.ComputeCount;
        _ = computed.Value;
        _ = computed.Value;

        Assert.Equal(computeCountAfterFirstRead, computed.ComputeCount);
    }

    [Fact]
    public void Computed_Chains_Propagate_Invalidation()
    {
        var source = new Signal<int>(1);
        var doubled = Computed.From(source, v => v * 2);
        var quadrupled = Computed.From(doubled, v => v * 2);

        Assert.Equal(4, quadrupled.Value);
        source.Value = 10;
        Assert.Equal(40, quadrupled.Value);
    }

    [Fact]
    public void Computed_Skips_Notification_When_Value_Unchanged()
    {
        var source = new Signal<int>(1);
        var parity = Computed.From(source, v => v % 2);
        _ = parity.Value;
        var versionBefore = parity.Version;

        source.Value = 3; // parity unchanged
        _ = parity.Value;

        Assert.Equal(versionBefore, parity.Version);
    }

    [Fact]
    public void Computed_Rejects_Foreign_Signal_Implementations()
    {
        var foreign = new ForeignSignal();
        Assert.Throws<ArgumentException>(() => Computed.From(foreign, v => v));
    }

    private sealed class ForeignSignal : IReadOnlySignal<int>
    {
        public int Value => 0;

        public long Version => 0;

        public IDisposable Subscribe(Action onChanged) => throw new NotSupportedException();
    }
}
