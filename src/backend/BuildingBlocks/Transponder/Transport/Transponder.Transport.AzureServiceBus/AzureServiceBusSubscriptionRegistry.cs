namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>Tracks CLR contracts this host can deserialize (topic subscription receives all; routing is in-process).</summary>
public sealed class AzureServiceBusSubscriptionRegistry
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
