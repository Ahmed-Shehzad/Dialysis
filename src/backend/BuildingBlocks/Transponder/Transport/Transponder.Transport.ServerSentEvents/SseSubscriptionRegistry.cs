namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Tracks CLR contracts this host can deserialize from the SSE stream.</summary>
public sealed class SseSubscriptionRegistry
{
    private readonly Dictionary<string, Type> _routingKeyToType = new(StringComparer.Ordinal);

    public void AddMessageType<TMessage>()
        where TMessage : class
    {
        var type = typeof(TMessage);
        var key = type.FullName ?? type.Name;
        _routingKeyToType[key] = type;
    }

    public IReadOnlyDictionary<string, Type> RoutingKeyToType => _routingKeyToType;

    public IReadOnlyCollection<string> RoutingKeys => [.. _routingKeyToType.Keys];
}
