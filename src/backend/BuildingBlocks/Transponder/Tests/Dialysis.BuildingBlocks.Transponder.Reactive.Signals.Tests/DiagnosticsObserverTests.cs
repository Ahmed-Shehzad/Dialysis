using Dialysis.BuildingBlocks.Transponder.Diagnostics;
using Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Diagnostics;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class DiagnosticsObserverTests
{
    private static (TransponderDiagnosticsSignals Signals, SignalTransponderStateObserver Observer) CreateGraph(
        TimeSpan? lagThreshold = null)
    {
        var options = Options.Create(new TransponderSignalsDiagnosticsOptions
        {
            OutboxLagThreshold = lagThreshold ?? TimeSpan.FromSeconds(30),
        });
        var signals = new TransponderDiagnosticsSignals(options);
        return (signals, new SignalTransponderStateObserver(signals));
    }

    [Fact]
    public void Transport_Transitions_Drive_Bus_Health_Computed()
    {
        var (signals, observer) = CreateGraph();
        Assert.Equal(TransponderBusHealth.Unknown, signals.BusHealth.Value);

        observer.OnTransportConnectionStateChanged("rabbitmq", TransponderTransportConnectionState.Connecting);
        Assert.Equal(TransponderBusHealth.Degraded, signals.BusHealth.Value);

        observer.OnTransportConnectionStateChanged("rabbitmq", TransponderTransportConnectionState.Connected);
        Assert.Equal(TransponderBusHealth.Healthy, signals.BusHealth.Value);
    }

    [Fact]
    public void Faulted_Transport_After_Connect_Marks_Bus_Down()
    {
        var (signals, observer) = CreateGraph();
        observer.OnTransportConnectionStateChanged("rabbitmq", TransponderTransportConnectionState.Connected);
        observer.OnTransportConnectionStateChanged(
            "rabbitmq",
            TransponderTransportConnectionState.Faulted,
            new InvalidOperationException("broker unreachable"));

        Assert.Equal(TransponderBusHealth.Down, signals.BusHealth.Value);
    }

    [Fact]
    public void Relay_Tick_Above_Threshold_Marks_Outbox_Lagging()
    {
        var (signals, observer) = CreateGraph(TimeSpan.FromSeconds(5));
        observer.OnTransportConnectionStateChanged("rabbitmq", TransponderTransportConnectionState.Connected);

        observer.OnOutboxRelayTick(new TransponderOutboxRelayObservation(
            "HisDbContext", IsLeader: true, BatchSize: 10, TimeSpan.FromSeconds(20), LastError: null));

        Assert.True(signals.OutboxLagging.Value);
        Assert.Equal(TransponderBusHealth.Degraded, signals.BusHealth.Value);

        observer.OnOutboxRelayTick(new TransponderOutboxRelayObservation(
            "HisDbContext", IsLeader: true, BatchSize: 0, TimeSpan.Zero, LastError: null));

        Assert.False(signals.OutboxLagging.Value);
        Assert.Equal(TransponderBusHealth.Healthy, signals.BusHealth.Value);
    }

    [Fact]
    public void Non_Leader_Tick_Never_Counts_As_Lagging()
    {
        var (signals, observer) = CreateGraph(TimeSpan.FromSeconds(5));

        observer.OnOutboxRelayTick(new TransponderOutboxRelayObservation(
            "HisDbContext", IsLeader: false, BatchSize: 0, TimeSpan.FromMinutes(10), LastError: null));

        Assert.False(signals.OutboxLagging.Value);
    }

    [Fact]
    public void Null_Observer_Instance_Ignores_All_Notifications()
    {
        var observer = NullTransponderStateObserver.Instance;

        observer.OnTransportConnectionStateChanged("rabbitmq", TransponderTransportConnectionState.Faulted, new InvalidOperationException());
        observer.OnOutboxRelayTick(new TransponderOutboxRelayObservation("X", true, 1, TimeSpan.MaxValue, "Boom"));

        // No observable state and no exception: the null observer is a true no-op.
        Assert.Same(NullTransponderStateObserver.Instance, observer);
    }
}
