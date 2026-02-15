using Transponder.Transports.Abstractions;
using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Default SSE transport host settings.
/// </summary>
public sealed class SseHostSettings : TransportHostSettings, ISseHostSettings
{
    public SseHostSettings(
        Uri address,
        ISseTopology? topology = null,
        int clientBufferCapacity = 128,
        TimeSpan? keepAliveInterval = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        Topology = topology ?? new SseTopology();
        ClientBufferCapacity = clientBufferCapacity <= 0 ? 128 : clientBufferCapacity;
        KeepAliveInterval = keepAliveInterval;
    }

    public ISseTopology Topology { get; }

    public int ClientBufferCapacity { get; }

    public TimeSpan? KeepAliveInterval { get; }
}
