using Dialysis.BuildingBlocks.Transponder.Diagnostics;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Diagnostics;

/// <summary>
/// The signals-backed <see cref="ITransponderStateObserver"/>: turns transport and relay
/// notifications into writes on the diagnostics signal graph. Per the observer contract this
/// is cheap (one CAS update per notification) and never throws.
/// </summary>
public sealed class SignalTransponderStateObserver : ITransponderStateObserver
{
    private readonly TransponderDiagnosticsSignals _signals;

    /// <summary>Creates the observer over the diagnostics signal graph.</summary>
    public SignalTransponderStateObserver(TransponderDiagnosticsSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        _signals = signals;
    }

    /// <inheritdoc />
    public void OnTransportConnectionStateChanged(string transportName, TransponderTransportConnectionState state, Exception? fault = null)
    {
        if (string.IsNullOrWhiteSpace(transportName))
        {
            return;
        }

        _signals.TransportStates.Update(states => states.SetItem(transportName, state));
    }

    /// <inheritdoc />
    public void OnOutboxRelayTick(TransponderOutboxRelayObservation tick)
    {
        if (tick is null)
        {
            return;
        }

        _signals.LastRelayTick.Value = tick;
    }
}
