namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Tracks CLR contracts this host can deserialize from the ingress stream.</summary>
public sealed class GrpcSubscriptionRegistry
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

    public IReadOnlyCollection<string> RoutingKeys => _routingKeyToType.Keys.ToArray();
}
