namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Collects message types this application listens for (routing keys = <see cref="Type.FullName"/>).
/// </summary>
public sealed class RabbitMqSubscriptionRegistry
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
