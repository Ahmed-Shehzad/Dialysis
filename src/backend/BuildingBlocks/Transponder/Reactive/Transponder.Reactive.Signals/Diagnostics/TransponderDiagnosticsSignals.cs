using System.Collections.Immutable;
using Dialysis.BuildingBlocks.Transponder.Diagnostics;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Diagnostics;

/// <summary>Aggregate health of this host's Transponder edge, derived from transport + relay signals.</summary>
public enum TransponderBusHealth
{
    /// <summary>No transport has reported yet.</summary>
    Unknown = 0,

    /// <summary>All transports connected; outbox not lagging.</summary>
    Healthy = 1,

    /// <summary>A transport is connecting/disconnected, or the outbox relay is lagging.</summary>
    Degraded = 2,

    /// <summary>At least one transport is faulted.</summary>
    Down = 3,
}

/// <summary>Tuning for the signals diagnostics pilot.</summary>
public sealed class TransponderSignalsDiagnosticsOptions
{
    /// <summary>Oldest-pending outbox age beyond which the relay counts as lagging.</summary>
    public TimeSpan OutboxLagThreshold { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// The diagnostics signal graph (audit §5): writable signals fed by
/// <see cref="SignalTransponderStateObserver"/>, computed health derived from them. Reads are
/// always coherent snapshots; the graph carries operational state only — never message
/// payloads or PHI.
/// </summary>
public sealed class TransponderDiagnosticsSignals
{
    private readonly TimeSpan _outboxLagThreshold;

    /// <summary>Builds the graph with the configured lag threshold.</summary>
    public TransponderDiagnosticsSignals(IOptions<TransponderSignalsDiagnosticsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _outboxLagThreshold = options.Value.OutboxLagThreshold;

        TransportStates = new Signal<ImmutableDictionary<string, TransponderTransportConnectionState>>(
            ImmutableDictionary<string, TransponderTransportConnectionState>.Empty);
        LastRelayTick = new Signal<TransponderOutboxRelayObservation?>(null);

        OutboxLagging = Computed.From(
            LastRelayTick,
            tick => tick is { IsLeader: true } && tick.OldestPendingAge > _outboxLagThreshold);

        BusHealth = Computed.From(
            TransportStates,
            OutboxLagging,
            static (states, lagging) =>
            {
                if (states.IsEmpty)
                {
                    return TransponderBusHealth.Unknown;
                }

                if (states.Values.Contains(TransponderTransportConnectionState.Faulted))
                {
                    return TransponderBusHealth.Down;
                }

                var allConnected = states.Values.All(s => s == TransponderTransportConnectionState.Connected);
                return !allConnected || lagging ? TransponderBusHealth.Degraded : TransponderBusHealth.Healthy;
            });
    }

    /// <summary>Current connection state per transport, keyed by transport name.</summary>
    public Signal<ImmutableDictionary<string, TransponderTransportConnectionState>> TransportStates { get; }

    /// <summary>The most recent outbox relay tick (null until the relay first reports).</summary>
    public Signal<TransponderOutboxRelayObservation?> LastRelayTick { get; }

    /// <summary>True when this replica leads the relay and the oldest pending row exceeds the threshold.</summary>
    public IReadOnlySignal<bool> OutboxLagging { get; }

    /// <summary>Aggregate bus health derived from transport states and outbox lag.</summary>
    public IReadOnlySignal<TransponderBusHealth> BusHealth { get; }
}
