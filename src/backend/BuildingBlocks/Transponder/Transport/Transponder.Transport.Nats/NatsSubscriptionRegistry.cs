namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Collects message types this host can deserialize (routing keys = <see cref="Type.FullName"/>).
/// </summary>
public sealed class NatsSubscriptionRegistry
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
