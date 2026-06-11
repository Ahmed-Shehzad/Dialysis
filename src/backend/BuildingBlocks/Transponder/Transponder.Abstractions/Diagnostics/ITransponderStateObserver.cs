namespace Dialysis.BuildingBlocks.Transponder.Diagnostics;

/// <summary>
/// Receives state-plane notifications from Transponder internals (transport connection
/// lifecycle, outbox relay ticks). Implementations MUST be cheap and MUST NOT throw — they are
/// called from connection lifecycles and the relay loop. The default is
/// <see cref="NullTransponderStateObserver.Instance"/>; <c>Transponder.Reactive.Signals</c>
/// provides a signals-backed implementation that derives bus health from these notifications.
/// </summary>
public interface ITransponderStateObserver
{
    /// <summary>Reports a transport's connection state transition.</summary>
    /// <param name="transportName">Stable transport identifier (e.g. <c>rabbitmq</c>).</param>
    /// <param name="state">The state the transport just entered.</param>
    /// <param name="fault">The triggering exception when <paramref name="state"/> is <see cref="TransponderTransportConnectionState.Faulted"/>.</param>
    void OnTransportConnectionStateChanged(string transportName, TransponderTransportConnectionState state, Exception? fault = null);

    /// <summary>Reports one outbox relay polling tick (leader and non-leader alike).</summary>
    void OnOutboxRelayTick(TransponderOutboxRelayObservation tick);
}

/// <summary>Connection lifecycle states a transport can report.</summary>
public enum TransponderTransportConnectionState
{
    /// <summary>No connection (initial, or after dispose).</summary>
    Disconnected = 0,

    /// <summary>A (re)connection attempt is in progress.</summary>
    Connecting = 1,

    /// <summary>Connected and ready to publish/consume.</summary>
    Connected = 2,

    /// <summary>The last connection attempt failed.</summary>
    Faulted = 3,
}

/// <summary>
/// Snapshot of one outbox relay polling tick. <paramref name="LastError"/> carries an exception
/// type name only — never message payloads or PHI.
/// </summary>
/// <param name="ContextName">The owning <c>DbContext</c> name (one relay per module database).</param>
/// <param name="IsLeader">Whether this replica held the per-database advisory lock for the tick.</param>
/// <param name="BatchSize">Unprocessed rows fetched this tick (0 when not leader).</param>
/// <param name="OldestPendingAge">Age of the oldest unprocessed row (zero when the outbox is empty or not leader).</param>
/// <param name="LastError">Exception type name when a row failed to publish this tick; otherwise null.</param>
public sealed record TransponderOutboxRelayObservation(
    string ContextName,
    bool IsLeader,
    int BatchSize,
    TimeSpan OldestPendingAge,
    string? LastError);
