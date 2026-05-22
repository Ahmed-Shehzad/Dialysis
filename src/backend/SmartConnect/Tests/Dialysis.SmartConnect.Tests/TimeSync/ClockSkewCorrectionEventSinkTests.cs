using Dialysis.SmartConnect.TimeSync;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TimeSync;

/// <summary>
/// Slice J3: <see cref="IClockSkewCorrectionEventSink"/> is wired into DI alongside the
/// monitor, defaults to a no-op so hosts without an audit publisher don't crash, and is
/// overridable so production hosts can wire a Transponder-backed sink.
/// </summary>
public sealed class ClockSkewCorrectionEventSinkTests
{
    [Fact]
    public void Add_Smart_Connect_Core_Registers_Null_Event_Sink_By_Default()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectCore();

        using var sp = services.BuildServiceProvider();
        var sink = sp.GetService<IClockSkewCorrectionEventSink>();

        Assert.NotNull(sink);
        Assert.IsType<NullClockSkewCorrectionEventSink>(sink);
    }

    [Fact]
    public void Custom_Sink_Override_Wins_Via_Try_Add_Singleton_Pattern()
    {
        var services = new ServiceCollection();
        // Host registers its own sink BEFORE AddSmartConnectCore so the TryAddSingleton
        // in the core extension steps aside.
        services.AddSingleton<IClockSkewCorrectionEventSink>(new CapturingSink());
        services.AddSmartConnectCore();

        using var sp = services.BuildServiceProvider();
        Assert.IsType<CapturingSink>(sp.GetRequiredService<IClockSkewCorrectionEventSink>());
    }

    [Fact]
    public async Task Null_Sink_Publish_Is_A_No_Op_Async()
    {
        var sink = new NullClockSkewCorrectionEventSink();
        var result = new ClockSkewCorrectionResult(
            SourceId: "MachineA@FAC",
            OriginalMessageTimestampUtc: new DateTime(2026, 5, 24, 13, 59, 0, DateTimeKind.Utc),
            ObservedAtUtc: new DateTime(2026, 5, 24, 13, 59, 59, DateTimeKind.Utc),
            ObservedSkew: TimeSpan.FromSeconds(59),
            CorrectedMessageTimestampUtc: new DateTime(2026, 5, 24, 13, 59, 59, DateTimeKind.Utc),
            WasCorrected: true,
            RejectionReason: null);

        // Doesn't throw; doesn't observe anything. The whole contract is "completes".
        await sink.PublishAsync(result, CancellationToken.None);
    }

    private sealed class CapturingSink : IClockSkewCorrectionEventSink
    {
        public List<ClockSkewCorrectionResult> Captured { get; } = [];

        public Task PublishAsync(ClockSkewCorrectionResult result, CancellationToken cancellationToken)
        {
            Captured.Add(result);
            return Task.CompletedTask;
        }
    }
}
