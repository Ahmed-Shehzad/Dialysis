using Transponder.Transports.Abstractions;
using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// Default SignalR transport host settings.
/// </summary>
public sealed class SignalRHostSettings : TransportHostSettings, ISignalRHostSettings
{
    public SignalRHostSettings(
        Uri address,
        ISignalRTopology? topology = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        Topology = topology ?? new SignalRTopology();
    }

    public ISignalRTopology Topology { get; }
}
