namespace Dialysis.BuildingBlocks.Transponder.Diagnostics;

/// <summary>
/// No-op <see cref="ITransponderStateObserver"/> used when no observer is registered, so call
/// sites can take an optional dependency without null checks at every notification.
/// </summary>
public sealed class NullTransponderStateObserver : ITransponderStateObserver
{
    private NullTransponderStateObserver()
    {
    }

    /// <summary>The singleton no-op instance.</summary>
    public static NullTransponderStateObserver Instance { get; } = new();

    /// <inheritdoc />
    public void OnTransportConnectionStateChanged(string transportName, TransponderTransportConnectionState state, Exception? fault = null)
    {
        // Deliberately empty.
    }

    /// <inheritdoc />
    public void OnOutboxRelayTick(TransponderOutboxRelayObservation tick)
    {
        // Deliberately empty.
    }
}
