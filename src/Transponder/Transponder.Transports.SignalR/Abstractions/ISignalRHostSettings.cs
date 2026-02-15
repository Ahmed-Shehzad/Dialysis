using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SignalR.Abstractions;

/// <summary>
/// Provides SignalR specific settings for creating a transport host.
/// </summary>
public interface ISignalRHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the SignalR topology conventions.
    /// </summary>
    ISignalRTopology Topology { get; }
}
